namespace AnalyticsService.Application.Services
{
    public class AdRuleResult
    {
        public List<string> Antecedents { get; set; } = new();
        public string Consequent { get; set; } = string.Empty;
        public double Support { get; set; }
        public double Confidence { get; set; }
    }

    public class AprioriService
    {
        public List<AdRuleResult> MineRules(List<HashSet<string>> transactions, double minSupport, double minConfidence)
        {
            var rules = new List<AdRuleResult>();
            var totalTransactions = transactions.Count;
            if (totalTransactions == 0) return rules;

            // 1. Đếm tần suất xuất hiện của từng Category đơn lẻ
            var itemFrequencies = new Dictionary<string, int>();
            foreach (var transaction in transactions)
            {
                foreach (var item in transaction)
                {
                    if (!itemFrequencies.ContainsKey(item)) itemFrequencies[item] = 0;
                    itemFrequencies[item]++;
                }
            }

            // 2. Đếm tần suất của các cặp Category (A và B cùng xuất hiện)
            var pairFrequencies = new Dictionary<string, int>();
            foreach (var transaction in transactions)
            {
                var items = transaction.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    for (int j = i + 1; j < items.Count; j++)
                    {
                        var pair = string.Compare(items[i], items[j]) < 0
                            ? $"{items[i]}|{items[j]}"
                            : $"{items[j]}|{items[i]}";

                        if (!pairFrequencies.ContainsKey(pair)) pairFrequencies[pair] = 0;
                        pairFrequencies[pair]++;
                    }
                }
            }

            // 3. Sinh luật (Rules Generation)
            foreach (var pair in pairFrequencies)
            {
                var countAB = pair.Value;
                var supportAB = (double)countAB / totalTransactions;

                if (supportAB < minSupport) continue;

                var items = pair.Key.Split('|');
                var itemA = items[0];
                var itemB = items[1];

                // Chiều A -> B
                var confAtoB = (double)countAB / itemFrequencies[itemA];
                if (confAtoB >= minConfidence)
                {
                    rules.Add(new AdRuleResult { Antecedents = new List<string> { itemA }, Consequent = itemB, Support = supportAB, Confidence = confAtoB });
                }

                // Chiều B -> A
                var confBtoA = (double)countAB / itemFrequencies[itemB];
                if (confBtoA >= minConfidence)
                {
                    rules.Add(new AdRuleResult { Antecedents = new List<string> { itemB }, Consequent = itemA, Support = supportAB, Confidence = confBtoA });
                }
            }

            return rules.OrderByDescending(r => r.Confidence).ThenByDescending(r => r.Support).ToList();
        }
    }
}