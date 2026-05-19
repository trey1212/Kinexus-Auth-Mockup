namespace KinexusMockup.Models;

public class KnowledgebankItem
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? LinkUrl { get; set; }
}

public class HomeViewModel
{
    public string? WelcomeMessage { get; set; }
    public string? SignetMessage { get; set; }

    public List<KnowledgebankItem> Knowledgebanks { get; set; } = new List<KnowledgebankItem>();
}
