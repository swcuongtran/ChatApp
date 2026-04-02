namespace SearchService.Api.Model
{
    public class AdCategoryDoc
    {
        public string Id { get; set; }
        public string CategoryName { get; set; }
        public float[] Embedding { get; set; }
    }
}
