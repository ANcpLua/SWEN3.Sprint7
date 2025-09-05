using System.Globalization;
using System.Text;
using System.Xml;

namespace SWEN3.Sprint7;

public static class AccessStatisticsXml
{
    public static class BatchConstants
    {
        public const string XmlRoot = "AccessStatistics";
        public const string XmlItem = "DocumentAccess";
        public const string AttrDate = "date";
        public const string AttrDocumentId = "documentId";
        public const string AttrCount = "count";
        public const string XmlDateFormat = "yyyy-MM-dd";

        public const string SampleFileNameFormat = "access_{0:yyyyMMdd}.xml";
    }

    public static (DateOnly Date, Dictionary<Guid, int> Aggregated) ParseStreaming(string filePath,
        CancellationToken ct)
    {
        var aggregated = new Dictionary<Guid, int>();

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreWhitespace = true
        };

        using var reader = XmlReader.Create(filePath, settings);

        if (!reader.ReadToFollowing(BatchConstants.XmlRoot))
            throw new XmlException($"Missing <{BatchConstants.XmlRoot}> root element.");

        var dateAttr = reader.GetAttribute(BatchConstants.AttrDate);
        if (!DateOnly.TryParseExact(dateAttr, BatchConstants.XmlDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            throw new XmlException(
                $"Invalid or missing '{BatchConstants.AttrDate}' attribute on <{BatchConstants.XmlRoot}>. Expected {BatchConstants.XmlDateFormat}.");
        }

        if (reader.ReadToDescendant(BatchConstants.XmlItem))
        {
            do
            {
                ct.ThrowIfCancellationRequested();

                var idAttr = reader.GetAttribute(BatchConstants.AttrDocumentId);
                var countAttr = reader.GetAttribute(BatchConstants.AttrCount);

                if (Guid.TryParse(idAttr, out var id) &&
                    int.TryParse(countAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) &&
                    count >= 0)
                {
                    aggregated[id] = aggregated.GetValueOrDefault(id) + count;
                }
            } while (reader.ReadToNextSibling(BatchConstants.XmlItem));
        }

        return (date, aggregated);
    }

    public static void WriteSample(string filePath, DateOnly date, IEnumerable<(Guid Id, int Count)> items)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var fs = File.Create(filePath);
        using var xw = XmlWriter.Create(fs, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        });

        xw.WriteStartDocument();
        xw.WriteStartElement(BatchConstants.XmlRoot);
        xw.WriteAttributeString(BatchConstants.AttrDate,
            date.ToString(BatchConstants.XmlDateFormat, CultureInfo.InvariantCulture));

        foreach (var (id, count) in items)
        {
            xw.WriteStartElement(BatchConstants.XmlItem);
            xw.WriteAttributeString(BatchConstants.AttrDocumentId, id.ToString());
            xw.WriteAttributeString(BatchConstants.AttrCount, count.ToString(CultureInfo.InvariantCulture));
            xw.WriteEndElement();
        }

        xw.WriteEndElement();
        xw.WriteEndDocument();
    }
}