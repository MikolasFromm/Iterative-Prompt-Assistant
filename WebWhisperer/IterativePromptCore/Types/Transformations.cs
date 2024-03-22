// user_custom definitions

namespace WebWhisperer.IterativePromptCore.Types
{
    public static class ListExtensions
    {
        /// <summary>
        /// Returns indices of given cells from the collection
        /// </summary>
        /// <param name="cells">List of cells from which to obtain the indices</param>
        /// <returns><see cref="IEnumerable{int}"/> collection of indices</returns>
        public static IEnumerable<int> GetIndexes(this IEnumerable<Cell> cells)
        {
            // get the index permutation after a rearrange operation
            var indexes =
                from cell in cells
                select cell.Index;
            var indexesList = indexes.ToList();

            //// set the default indexing
            //int index = 0;
            //foreach (var cell in cells)
            //    cell.Index = index++;

            return indexesList;
        }

        /// <summary>
        /// Sorts the data in the field and returns the indices of the sorted data
        /// </summary>
        /// <param name="field">Field to sort</param>
        /// <returns><see cref="IEnumerable{int}"/> collection of indices of the cells from the field</returns>
        public static IEnumerable<int> SortAndGetIndexes(this Field field, SortDirection sortDirection)
        {
            switch (sortDirection)
            {
                case SortDirection.Ascending:
                    field.Data.Sort((a, b) => a.CompareToTypeDependent(field.Header.Type, b));
                    break;
                case SortDirection.Descending:
                    field.Data.Sort((a, b) => b.CompareToTypeDependent(field.Header.Type, a));
                    break;
                default:
                    throw new ArgumentException("Unsupported sort direction");
            }

            return field.Data.GetIndexes();
        }

        /// <summary>
        /// Recreates a list of fields where specific header might be dropped and only those cells from the field which coresponds to the given indices are kept.
        /// </summary>
        /// <param name="fieldList"></param>
        /// <param name="fieldToIgnore"></param>
        /// <param name="indexes"></param>
        /// <returns><see cref="List{Field}"/> collection of rearranged Fields.</returns>
        public static List<Field> ReArrangeAndSelectByIndex(this List<Field> fieldList, IEnumerable<int> indexes, Header? fieldToIgnore = null)
        {
            for (int i = 0; i < fieldList.Count; i++)
            {
                if (fieldToIgnore is null || fieldList[i].Header != fieldToIgnore)
                {
                    Field newField = new() { Header = fieldList[i].Header };
                    int newIndex = 0;
                    foreach (var index in indexes)
                    {
                        fieldList[i].Data[index].Index = newIndex;
                        newField.Data.Add(fieldList[i].Data[index]);
                        newIndex++;
                    }
                    fieldList[i] = newField;
                }

                // set the default indexing
                int cell_index = 0;
                foreach (var cell in fieldList[i].Data)
                    cell.Index = cell_index++;
            }
            return fieldList;
        }
    }

    public static class TransformationFactory
    {
        /// <summary>
        /// Only returns empty transformation class for obtaining the corrent moves and arguments.
        /// </summary>
        /// <param name="transformation">String representation of the transformation</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">When unknown transformation given.</exception>
        public static ITransformation Create(string transformation)
        {
            switch (transformation)
            {
                case "Empty":
                    return new EmptyTransformation();
                case "DropColumn":
                    return new DropColumnTransformation();
                case "SortBy":
                    return new SortByTransformation();
                case "GroupBy":
                    return new GroupByTransformation();
                case "FilterBy":
                    return new FilterByTransformation();
                default:
                    throw new ArgumentException("Unknown transformation");
            }
        }

        public static ITransformation CreateByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return new EmptyTransformation();
                case 1:
                    return new DropColumnTransformation();
                case 2:
                    return new SortByTransformation();
                case 3:
                    return new GroupByTransformation();
                case 4:
                    return new FilterByTransformation();
                default:
                    throw new ArgumentException("Unknown transformation");
            }
        }

        /// <summary>
        /// Transformation builder, which already expects all input arguments in given order and in corrent format.
        /// It is expected that user first calls <see cref="Create(string)"/> to obtain all moves and arguments.
        /// </summary>
        /// <param name="transformation"><see cref="string"/> representation of a transformation.</param>
        /// <param name="args">All arguments given for the transformation.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">When wrong arguments given.</exception>
        public static ITransformation BuildTransformation(string transformation, params string[] args)
        {
            switch (transformation)
            {
                case StaticNames.Empty:
                case "0":
                    return new EmptyTransformation();

                case StaticNames.DropColumn:
                case "1":
                    if (args.Length < 1)
                        throw new ArgumentException("Not enough arguments for DropColumn transformation");

                    HashSet<string> dropColumns = new();

                    foreach (var arg in args)
                    {
                        if (!string.IsNullOrEmpty(arg))
                            dropColumns.Add(arg);
                    }

                    return new DropColumnTransformation(dropColumns);

                case StaticNames.SortBy:
                case "2":
                    if (args.Length < 2)
                        throw new ArgumentException("Not enough arguments for SortBy transformation");

                    SortDirection direction = SortDirection.Ascending;
                    string headerName = args[0]; // gets the header of the field by which to sort

                    if (args[1] == StaticNames.Ascending) // if ascending or descending
                        direction = SortDirection.Ascending;
                    else if (args[1] == StaticNames.Descending) // if ascending or descending
                        direction = SortDirection.Descending;
                    else
                        throw new ArgumentException($"SortDirection \"{args[1]}\" unrecognized");

                    return new SortByTransformation(headerName, direction);

                case StaticNames.GroupBy:
                case "3":
                    if (args.Length < 3)
                        throw new ArgumentException("Not enough arguments for GroupBy transformation");

                    Agregation agregation = new();
                    string targetHeader = string.Empty;
                    HashSet<string> groups = new();


                    targetHeader = args[0]; // gets the header of the field by which to group
                    if (args[1] == StaticNames.Sum)
                        agregation = Agregation.Sum;
                    else if (args[1] == StaticNames.Average)
                        agregation = Agregation.Mean;
                    else if (args[1] == StaticNames.Concat)
                        agregation = Agregation.ConcatValues;
                    else if (args[1] == StaticNames.CountDistinct)
                        agregation = Agregation.CountDistinct;
                    else if (args[1] == StaticNames.CountAll)
                        agregation = Agregation.CountAll;
                    else if (args[1] == StaticNames.GroupKey)
                        agregation = Agregation.GroupKey;
                    else
                        throw new ArgumentException($"Agregation \"{args[1]}\" not supported");

                    for (int i = 2; i < args.Length; i++)
                        groups.Add(args[i]);

                    return new GroupByTransformation(groups, agregation, targetHeader);

                case StaticNames.FilterBy:
                case "4":
                    if (args.Length < 3)
                        throw new ArgumentException("Not enough arguments for FilterBy transformation");

                    FilterCondition filter = new();

                    filter.SourceHeaderName = args[0];

                    if (args[1] == StaticNames.Equals)
                    {
                        filter.Relation = Relation.Equals;
                        filter.Condition = args[2];
                    }
                    else if (args[1] == StaticNames.NotEquals)
                    {
                        filter.Relation = Relation.NotEquals;
                        filter.Condition = args[2];
                    }
                    else if (args[1] == StaticNames.LessThan)
                    {
                        filter.Relation = Relation.LessThan;
                        filter.Condition = args[2];
                    }
                    else if (args[1] == StaticNames.GreaterThan)
                    {
                        filter.Relation = Relation.GreaterThan;
                        filter.Condition = args[2];
                    }
                    else
                    {
                        throw new ArgumentException($"Operation \"{args[0]}\" not supported");
                    }

                    return new FilterByTransformation(filter);

                default:
                    throw new ArgumentException($"Transformation \"{transformation}\" not supported");
            }
        }
    }

    public interface ITransformation
    {
        public TransformationType Type { get; }

        public bool HasArguments { get; }

        public bool HasFollowingHumanArguments { get; }

        public int TotalStepsNeeded { get; }

        /// <summary>
        /// Makes the real final transformation on the given field.
        /// </summary>
        /// <param name="input_list"></param>
        /// <returns></returns>
        public List<Field> PerformTransformation(List<Field> input_list);

        /// <summary>
        /// Makes the transformation only with <see cref="EmptyField"/>, which is used to get all future transformations.
        /// </summary>
        /// <param name="list">Current dataset</param>
        /// <returns></returns>
        public List<EmptyField> Preprocess(List<EmptyField> list);

        /// <summary>
        /// Returns string represenation of the function operator assigned to the transformation.
        /// Should be given to the OpenAI api to get all possibilities.
        /// </summary>
        /// <returns><see cref="string"/> representation of the transformation.</returns>
        public string GetTransformationName();

        /// <summary>
        /// Returns human instructions for the next moves.
        /// </summary>
        /// <returns></returns>
        public string GetNextMovesInstructions();

        /// <summary>
        /// Returns all possible next moves when the transformation is invoked.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields);

        /// <summary>
        /// Returns human instructions for the arguments that should be given.
        /// </summary>
        /// <returns></returns>
        public string GetArgumentsInstructions();

        /// <summary>
        /// Returns all possible arguments related to the possible moves.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetArguments();

        /// <summary>
        /// Returns the argument at the given index. Allows the transformation class to adjust the following options based on the requested argument.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetArgumentAt(int index);

        public string GetFollowingHumanArgumentsInstructions();
    }

    public class EmptyTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.Empty;

        public bool HasArguments => false;

        public bool HasFollowingHumanArguments => false;

        public int TotalStepsNeeded => 0;

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            return input_fields;
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list;
        }

        public string GetTransformationName() => "Empty";

        public string GetNextMovesInstructions() => "Empty transformation has no next moves";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields) => new List<string>();

        public string GetArgumentsInstructions() => string.Empty;

        public IEnumerable<string> GetArguments() => new List<string>();

        public string GetArgumentAt(int index) => string.Empty;

        public string GetFollowingHumanArgumentsInstructions() => string.Empty;

    }

    public class DropColumnTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.DropColumns;

        public bool HasArguments => false;

        public bool HasFollowingHumanArguments => false;

        public int TotalStepsNeeded => 1;

        public HashSet<string> DropHeaderNames { get; set; } = new HashSet<string>();

        public DropColumnTransformation(HashSet<string> dropHeaderNames)
        {
            DropHeaderNames = dropHeaderNames;
        }

        internal DropColumnTransformation() { }

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            var selected_fields =
                 from field in input_fields
                 where !DropHeaderNames.Contains(field.Header.Name)
                 select field;
            return selected_fields.ToList();
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            var selected_fields =
                 from field in list
                 where !DropHeaderNames.Contains(field.Header.Name)
                 select field;
            return selected_fields.ToList();
        }

        public string GetTransformationName() => "DropColumn";

        public string GetNextMovesInstructions() => "Select Field.Header.Name to drop";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields)
        {
            var moves = new List<string>();
            foreach (var field in fields)
            {
                moves.Add(field.Header.Name);
            }
            return moves;
        }

        public string GetArgumentsInstructions() => string.Empty;

        public IEnumerable<string> GetArguments() => new List<string>();

        public string GetArgumentAt(int index) => string.Empty;

        public string GetFollowingHumanArgumentsInstructions() => string.Empty;
    }

    public class SortByTransformation : ITransformation
    {
        public bool HasArguments => true;

        public bool HasFollowingHumanArguments => false;

        public int TotalStepsNeeded => 2;

        public TransformationType Type => TransformationType.SortBy;

        public SortDirection Direction { get; set; }

        public string SortByHeaderName { get; set; }

        private static readonly List<string> _argumentsList = new List<string> { StaticNames.Ascending, StaticNames.Descending };

        public SortByTransformation(string sortByHeaderName, SortDirection direction)
        {
            SortByHeaderName = sortByHeaderName;
            Direction = direction;
        }

        internal SortByTransformation() { }

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            Field? source_field = input_fields.FirstOrDefault(x => x.Header.Name == SortByHeaderName);
            if (source_field is not null)
            {
                var indexes = source_field.SortAndGetIndexes(Direction); // sort the given field and get the sorted indices

                return input_fields.ReArrangeAndSelectByIndex(indexes, source_field.Header); // re-arrange all fields by the sorted indices and ignore the one already sorted
            }
            else
            {
                throw new ArgumentException("Field to SortBy not found.");
            }
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list; // practically the set remains the same, only order changes
        }

        public string GetTransformationName() => "SortBy";

        public string GetNextMovesInstructions() => "Choose one Field from the list below, by which you want to sort the dataset";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields)
        {
            var moves = new List<string>();
            foreach (var field in fields)
            {
                moves.Add(field.Header.Name);
            }
            return moves;
        }

        public string GetArgumentsInstructions() => "Choose whether the sorting should be ascending or descending";

        public IEnumerable<string> GetArguments() => _argumentsList;

        public string GetArgumentAt(int index)
        {
            if (index < 0 || index >= _argumentsList.Count)
                throw new ArgumentOutOfRangeException($"Index \"{index}\" in {nameof(GetArgumentAt)} out of range");
            return _argumentsList[index];
        }

        public string GetFollowingHumanArgumentsInstructions() => string.Empty;
    }

    public class GroupByTransformation : ITransformation
    {
        public bool HasArguments => true;

        public bool HasFollowingHumanArguments { get; set; } = false; // might be true if the arguments requires so

        public int TotalStepsNeeded => 2;

        public TransformationType Type => TransformationType.GroupBy;

        public HashSet<string> StringsToGroup { get; set; } = new HashSet<string>();

        public Agregation GroupAgregation { get; set; }

        public string TargetHeaderName { get; set; }

        private static readonly List<string> _argumentsList = new List<string> 
        { 
            StaticNames.Sum, 
            StaticNames.Average, 
            StaticNames.Concat, 
            StaticNames.CountDistinct, 
            StaticNames.CountAll, 
            //StaticNames.GroupKey 
        };

        public GroupByTransformation(HashSet<string> stringsToGroup, Agregation groupAgregation, string targetHeaderName)
        {
            StringsToGroup = stringsToGroup;
            GroupAgregation = groupAgregation;
            TargetHeaderName = targetHeaderName;
            HasFollowingHumanArguments = false; // default value
        }

        internal GroupByTransformation() { }

        public List<Field> PerformTransformation(List<Field> fields)
        {
            Field? targetField = fields.FirstOrDefault(x => x.Header.Name == TargetHeaderName);
            if (targetField is null) throw new ArgumentException("Field to GroupBy not found.");


            // Identifying groups in the target field
            var groups = targetField.Data
                .GroupBy(cell => cell.Content)
                .ToDictionary(group => group.Key, group => group.ToList());

            // Initialize a new list for transformed fields
            List<Field> transformedFields = new List<Field>();

            foreach (var currentField in fields)
            {

                Field transformedField = new Field
                {
                    Header = currentField.Header,
                    Data = new List<Cell>()
                };

                int rowIndex = 0;

                // If the current field is the target grouping field, handle it specifically
                if (currentField.Header.Name == TargetHeaderName)
                {
                    foreach (var groupKey in groups.Keys)
                    {
                        transformedField.Data.Add(new Cell { Content = groupKey, Index = rowIndex++ });
                    }
                    // Add the transformed field for the grouping column and skip the aggregation logic
                    transformedFields.Add(transformedField);
                    continue; // Move to the next field without entering the aggregation logic
                }

                // Aggregation logic
                foreach (var group in groups)
                {
                    var cells = group.Value.GetIndexes()
                        .Select(index => currentField.Data[index])
                        .ToList();

                    switch (GroupAgregation)
                    {
                        case Agregation.CountAll:
                            int countAll = cells.Count;
                            transformedField.Data.Add(new Cell { Content = countAll.ToString(), Index = rowIndex++ });
                            break;

                        case Agregation.CountDistinct:
                            int countDistinct = cells.Select(cell => cell.Content).Distinct().Count();
                            transformedField.Data.Add(new Cell { Content = countDistinct.ToString(), Index = rowIndex++ });
                            break;

                        case Agregation.ConcatValues:
                            string concatValues = string.Join(", ", cells.Select(cell => cell.Content));
                            transformedField.Data.Add(new Cell { Content = concatValues, Index = rowIndex++ });
                            break;

                        case Agregation.Sum:
                            double sum = cells
                                .Where(cell => double.TryParse(cell.Content, out double _))
                                .Sum(cell => double.Parse(cell.Content));
                            transformedField.Data.Add(new Cell { Content = sum.ToString(), Index = rowIndex++ });
                            break;

                        case Agregation.Mean:
                            var validNumbers = cells
                                .Select(cell => double.TryParse(cell.Content, out double num) ? num : (double?)null)
                                .Where(num => num.HasValue)
                                .Select(cell => cell.Value)
                                .ToList();
                            double mean = validNumbers.Any() ? validNumbers.Average() : 0;
                            transformedField.Data.Add(new Cell { Content = mean.ToString("F2"), Index = rowIndex++ }); // "F2" for two decimal places
                            break;

                        default:
                            throw new NotImplementedException($"Aggregation {GroupAgregation} not implemented.");
                    }
                }
                transformedFields.Add(transformedField);
            }

            // Return the transformed fields with adjusted rows per aggregation logic
            return transformedFields;
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            var preprocessed = new List<EmptyField>();
            switch (GroupAgregation)
            {
                case Agregation.CountAll:
                case Agregation.CountDistinct:
                case Agregation.Sum:
                case Agregation.Mean:
                    foreach (var field in list)
                    {
                        preprocessed.Add(new EmptyField 
                        {
                            Header = new Header(
                                field.Header.Name,
                                FieldDataType.Number,
                                field.Header.Index)
                        });
                    }
                    return preprocessed;
                //case Agregation.GroupKey:
                case Agregation.ConcatValues:
                    return preprocessed;

                default:
                    throw new ArgumentException("Unknown Agregation");
            }
        }

        public string GetTransformationName() => "GroupBy";

        public string GetNextMovesInstructions() => "Choose one Field.Header.Name from the dataset, by which you want to group the dataset";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields)
        {
            var moves = new List<string>();
            foreach (var item in fields)
            {
                moves.Add(item.Header.Name);
            }
            return moves;
        }

        public string GetArgumentsInstructions() => "Choose one of the following Agregations you want to apply on the grouped dataset";

        public IEnumerable<string> GetArguments() => _argumentsList;

        public string GetArgumentAt(int index)
        {
            if (index < 0 || index >= _argumentsList.Count)
                throw new ArgumentOutOfRangeException($"Index \"{index}\" in {nameof(GetArgumentAt)} out of range");

            return _argumentsList[index];
        }

        public string GetFollowingHumanArgumentsInstructions() => string.Empty;
    }

    public class FilterByTransformation : ITransformation
    {
        public bool HasArguments => true;

        public bool HasFollowingHumanArguments => true;

        public int TotalStepsNeeded => 3;

        public TransformationType Type => TransformationType.FilterBy;

        public FilterCondition FilterCondition { get; set; }

        private static readonly List<string> _argumentsList = new List<string> { StaticNames.Equals, StaticNames.NotEquals, StaticNames.LessThan, StaticNames.GreaterThan };

        public FilterByTransformation(FilterCondition filterCondition)
        {
            FilterCondition = filterCondition;
        }

        internal FilterByTransformation() { }

        public List<Field> PerformTransformation(List<Field> fields)
        {
            Field? field = fields.FirstOrDefault(x => x.Header.Name == FilterCondition.SourceHeaderName);
            if (field is not null)
            {
                switch (FilterCondition.Relation)
                {
                    case Relation.Equals:
                        var eq_indexes = from cell in field.Data where cell.CompareTo_TypeDependent(field.Header.Type, FilterCondition.Condition) == 0 select cell.Index;
                        return fields.ReArrangeAndSelectByIndex(eq_indexes);

                    case Relation.NotEquals:
                        var neq_indexes = from cell in field.Data where cell.CompareTo_TypeDependent(field.Header.Type, FilterCondition.Condition) != 0 select cell.Index;
                        return fields.ReArrangeAndSelectByIndex(neq_indexes);

                    case Relation.LessThan:
                        var lt_indexes = from cell in field.Data where cell.CompareTo_TypeDependent(field.Header.Type, FilterCondition.Condition) < 0 select cell.Index;
                        return fields.ReArrangeAndSelectByIndex(lt_indexes); ;

                    case Relation.GreaterThan:
                        var gt_indexes = from cell in field.Data where cell.CompareTo_TypeDependent(field.Header.Type, FilterCondition.Condition) > 0 select cell.Index;
                        return fields.ReArrangeAndSelectByIndex(gt_indexes); ;

                    case Relation.InRange:
                        throw new NotImplementedException("Relation.InRange not implemented");

                    default:
                        throw new ArgumentException("Unknown Relation operator given.");
                }
            }
            else
            {
                throw new ArgumentException("Field to FilterBy not found.");
            }
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list;
        }

        public string GetTransformationName() => "FilterBy";

        public string GetNextMovesInstructions() => "Choose one Field.Header.Name from the dataset, by which you want to filter the dataset";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields)
        {
            var moves = new List<string>();
            foreach (var item in fields)
            {
                moves.Add(item.Header.Name);
            }
            return moves;
        }

        public string GetArgumentsInstructions() => "Choose one of the following Relations you want to apply on the filtered dataset";

        public IEnumerable<string> GetArguments() => _argumentsList;

        public string GetArgumentAt(int index)
        {
            if (index < 0 || index >= _argumentsList.Count)
                throw new ArgumentOutOfRangeException($"Index \"{index}\" in {nameof(GetArgumentAt)} out of range");
            return _argumentsList[index];
        }

        public string GetFollowingHumanArgumentsInstructions() => "Write down the right side of the relation.";
    }

    public static class Transformator
    {
        public static List<Field> TransformFields(List<Field> fields, IEnumerable<ITransformation> transformations)
        {
            var transformedFields = new List<Field>(fields);

            foreach (var transformation in transformations)
            {
                transformedFields = transformation.PerformTransformation(transformedFields);
            }
            return transformedFields;
        }
    }
}