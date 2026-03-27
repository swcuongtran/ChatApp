using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyticsService.Infrastructure.MongoDb.Documents
{
    public class AdRuleDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public List<string> Antecedents { get; set; } = new();
        public string Consequent { get; set; } = string.Empty;
        public double Support { get; set; }
        public double Confidence { get; set; }
    }
}
