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

    int i = 0;
    while (await reader.ReadAsync())
    {
        if (reader.NodeType == XmlNodeType.Element && reader.Name == "page")
        {
            reader.ReadToDescendant("title");
            await reader.ReadAsync();
            var title = await reader.GetValueAsync();

            reader.ReadToFollowing("text");
            await reader.ReadAsync();
            var text = await reader.GetValueAsync();

            Console.WriteLine("Title: {0}", title);
            Console.WriteLine("Text: {0}", text);
            Console.WriteLine();
        }
        

        i++;
        if (i > 100_000)
        {
            break;
        }
    }
}


