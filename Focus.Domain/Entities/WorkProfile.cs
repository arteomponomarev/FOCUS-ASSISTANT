using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public sealed class WorkProfile
{
    private readonly List<ProcessRule> _rules = [];

    private WorkProfile()
    {
    }

    public WorkProfile(Guid userId, string name, string? description, int focusMinutes, int breakMinutes)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Rename(name, description, focusMinutes, breakMinutes);
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public int FocusMinutes { get; private set; }
    public int BreakMinutes { get; private set; }
    public IReadOnlyList<ProcessRule> Rules => _rules.AsReadOnly();

    public void Rename(string name, string? description, int focusMinutes, int breakMinutes)
    {
        Name = name;
        Description = description;
        FocusMinutes = focusMinutes;
        BreakMinutes = breakMinutes;
    }

    public void ReplaceRules(IReadOnlyDictionary<string, ProcessCategory> rules)
    {
        _rules.RemoveAll(rule => !rules.ContainsKey(rule.ProcessName));
        foreach (var (name, category) in rules)
        {
            var existing = _rules.Find(rule => rule.ProcessName == name);
            if (existing is null)
                _rules.Add(new ProcessRule(Id, name, category));
            else
                existing.SetCategory(category);
        }
    }
}

public sealed class ProcessRule
{
    private ProcessRule()
    {
    }

    internal ProcessRule(Guid profileId, string name, ProcessCategory category)
    {
        WorkProfileId = profileId;
        ProcessName = name;
        Category = category;
    }

    public Guid WorkProfileId { get; private set; }
    public string ProcessName { get; private set; } = "";
    public ProcessCategory Category { get; private set; }

    internal void SetCategory(ProcessCategory category) => Category = category;
}
