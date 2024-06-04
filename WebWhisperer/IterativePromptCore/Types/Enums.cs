namespace WebWhisperer.IterativePromptCore.Types.Enums
{
    public enum FieldDataType
    {
        Bool,
        String,
        Number,
        Date
    };

    public enum Selection
    {
        Take,
        Skip
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public enum FilterLogicalOperator
    {
        And,
        Or
    }

    public enum Relation
    {
        Equals,
        NotEquals,
        LessThan,
        GreaterThan,
        InRange
    }

    public enum Agregation
    {
        GroupKey,
        CountAll,
        CountDistinct,
        ConcatValues,
        Sum,
        Mean
    }

    public enum TransformationType
    {
        Empty = 0,
        DropColumns = 1,
        SortBy = 2,
        GroupBy = 3,
        FilterBy = 4
    }

    public enum CommunicationAgentMode
    {
        User,
        AIBot,
        AIBotWebWhisper
    }
}