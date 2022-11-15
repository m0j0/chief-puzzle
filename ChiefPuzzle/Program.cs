using Microsoft.Data.Sqlite;
using System.Xml;

await FillDb();




// https://en.wiktionary.org/wiki/Help:FAQ#Downloading_Wiktionary
async Task FillDb()
{
    await using var stream = File.OpenRead(@"C:\Users\maslov.n\Desktop\ruwiktionary-20221101-pages-articles-multistream.xml");

    var settings = new XmlReaderSettings
    {
        Async = true
    };

    using var reader = XmlReader.Create(stream, settings);

    await using var dbConnection = new SqliteConnection(@"Data Source=C:\source\_outside\chief-puzzle\dict.db");
    await dbConnection.OpenAsync();

    await using var cmd = new SqliteCommand();
    cmd.Connection = dbConnection;

    cmd.CommandText = "DROP TABLE IF EXISTS dict";
    cmd.ExecuteNonQuery();

    //cmd.CommandText = "DROP INDEX IF EXISTS word_index";
    //cmd.ExecuteNonQuery();

    cmd.CommandText = "CREATE TABLE dict(word TEXT, meaning TEXT, full TEXT)";
    cmd.ExecuteNonQuery();


    for (int i = 0; i < 100_000 && await reader.ReadAsync(); i++)
    {
        if (reader.NodeType != XmlNodeType.Element || reader.Name != "page")
        {
            continue;
        }

        reader.ReadToDescendant("title");
        await reader.ReadAsync();
        var title = await reader.GetValueAsync();

        reader.ReadToFollowing("text");
        await reader.ReadAsync();
        var text = await reader.GetValueAsync();

        if (text.StartsWith("#REDIRECT"))
        {
            continue;
        }
        
        var meaning = GetMeaning(text);

        static string GetMeaning(string t)
        {
            string значение = "==== Значение ====";
            int значениеLength = значение.Length + 1;

            var start = t.IndexOf(значение, StringComparison.OrdinalIgnoreCase);
            if (start == -1)
            {
                значение = "==== Семантические свойства ====";
                start = t.IndexOf(значение, StringComparison.OrdinalIgnoreCase);

                if (start == -1)
                {
                    return t;
                }
            }
            
            var end = t.IndexOf("===", start + значениеLength, StringComparison.OrdinalIgnoreCase);
            if (end == -1)
            {
                return t;
            }

            return t.Substring(start + значениеLength, end - start - значениеLength).Trim();
        }

        cmd.Parameters.Clear();
        cmd.CommandText = "INSERT INTO dict(word, meaning, full) VALUES(@title, @meaning, @full)"; ;
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@meaning", meaning);
        cmd.Parameters.AddWithValue("@full", text);
        cmd.ExecuteNonQuery();
    }

    cmd.CommandText = "CREATE INDEX word_index ON dict (word);";
    cmd.ExecuteNonQuery();
}
