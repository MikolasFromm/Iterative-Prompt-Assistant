using OpenAI_API;
using WebWhisperer.IterativePromptCore.Communication;
using WebWhisperer.IterativePromptCore.Model;
using WebWhisperer.IterativePromptCore.Types.DataTypes;
using WebWhisperer.IterativePromptCore.Types.Transformations;

namespace WebWhisperer.IterativePromptCore.Query
{
    public class QueryAgent : IQueryAgent
    {
        private ICommunicationAgent _communicationAgent;

        private List<EmptyField> _response = new List<EmptyField>();

        private IEnumerable<ITransformation> _possibleTransformations = new List<ITransformation>()
        {
            new EmptyTransformation(),
            new DropColumnTransformation(),
            new SortByTransformation(),
            new GroupByTransformation(),
            new FilterByTransformation()
        };

        #region Factory methods
        public static QueryAgent CreateUserQueryAgent(List<Field> fields, bool verbose = true)
        {
            return new QueryAgent(fields, verbose);
        }

        public static QueryAgent CreateOpenAIQueryAgent(OpenAIAPI api, List<Field> fields, bool verbose = true)
        {
            return new QueryAgent(api, fields, verbose);
        }

        public static QueryAgent CreateOpenAIServerQueryAgent(OpenAIAPI api, bool verbose = true)
        {
            return new QueryAgent(api, verbose);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor for creating query with OpenAI chatbot
        /// </summary>
        /// <param name="api"></param>
        /// <param name="verbose"></param>
        public QueryAgent(OpenAIAPI api, List<Field> fields, bool verbose = true)
        {
            _response = new List<EmptyField>(fields);
            _communicationAgent = new CommunicationAgent(api, verbose);
        }

        /// <summary>
        /// Default constructor for creating user query, where user creates the response.
        /// </summary>
        /// <param name="verbose"></param>
        public QueryAgent(List<Field> fields, bool verbose = true)
        {
            _response = new List<EmptyField>(fields);
            _communicationAgent = new CommunicationAgent(verbose);
        }

        /// <summary>
        /// Default constructor for Server API communication
        /// </summary>
        public QueryAgent(OpenAIAPI api, bool verbose = true)
        {
            _communicationAgent = new CommunicationAgent(api, verbose);
        }

        #endregion

        public void AddUserQuery(string userQuery = null)
        {
            if (_communicationAgent is not null)
                _communicationAgent.AddUserQuery(userQuery);
        }

        public void StartNewQueryAttempt()
        {
            if (_communicationAgent is not null)
                _communicationAgent.FlushCurrentChat();
        }

        public QueryViewModel PerformQuerying(IList<string> queryItems, IList<Field> fields)
        {
            // refresh with the initial table
            _response = new List<EmptyField>(fields);

            var responseQueryModel = new QueryViewModel();

            bool firstQueryItem = true;

            ITransformation transformationCandidate = null;
            int totalStepsMade = 0;

            var querySoFar = string.Join('.', queryItems);

            if (_communicationAgent.Verbose)
            {
                Console.WriteLine();
                Console.WriteLine($"---> Query: {querySoFar}");
            }

            // always remove the history of the previous query
            _communicationAgent.FlushCurrentChat();

            while (queryItems.Any() || firstQueryItem)
            {
                totalStepsMade = 0;
                ITransformation generatedTransformation = null;
                try
                {
                    string nextMove = string.Empty;
                    string transformationName = string.Empty;
                    string firstArgument = string.Empty;
                    string secondArgument = string.Empty;

                    // Print all possible transformations
                    responseQueryModel.NextMoves = _communicationAgent.CreateNextQuestion($"---> Choose next transformation: ", from transformation in _possibleTransformations select $"{transformation.GetTransformationName()}");

                    // dont skip the previous queryItem when at the beginning
                    if (!firstQueryItem)
                        queryItems.RemoveAt(0);
                    else
                        firstQueryItem = false;

                    var nextQueryItem = queryItems.FirstOrDefault();
                    var index = responseQueryModel.NextMoves.ToList().IndexOf(nextQueryItem);

                    // Gets the next transformation name    
                    transformationName = _communicationAgent.GetResponse(querySoFar, nextQueryItem, index);
                    responseQueryModel.AddBotSuggestion(transformationName);


                    if (string.IsNullOrEmpty(transformationName))
                        break;

                    // Try to parse the string into number
                    if (!int.TryParse(transformationName, out int transformationIndex))
                    {
                        _communicationAgent.ErrorMessage($"Invalid input, you must enter only an integer. Please try again.");
                        _communicationAgent.Indent();
                        continue;
                    }

                    // Check if the number is in range
                    if (transformationIndex >= _possibleTransformations.Count())
                    {
                        _communicationAgent.ErrorMessage($"Invalid input out of range. You must choose from the selection above. Please try again.");
                        _communicationAgent.Indent();
                        continue;
                    }

                    // check is over
                    if (transformationIndex == 0)
                        break;

                    // Create the transformation candidate
                    transformationCandidate = TransformationFactory.CreateByIndex(transformationIndex);

                    IEnumerable<string> nextMoves = null;

                    // loop until getting satisfying answer
                    while (queryItems.Any())
                    {
                        // Get the primary instruction for the transformation
                        var nextPossibleMoves = transformationCandidate.GetNextMoves(_response);
                        nextMoves = _communicationAgent.CreateNextQuestion($"---> {transformationCandidate.GetNextMovesInstructions()}", nextPossibleMoves);

                        // obtain the message
                        queryItems.RemoveAt(0);

                        responseQueryModel.NextMoves = nextMoves;

                        nextQueryItem = queryItems.FirstOrDefault();
                        index = responseQueryModel.NextMoves.ToList().IndexOf(nextQueryItem);

                        var response = _communicationAgent.GetResponse(querySoFar, nextQueryItem, index);
                        totalStepsMade++;
                        responseQueryModel.AddBotSuggestion(response);

                        // check the message
                        bool isNotNullOrEmpty = !string.IsNullOrEmpty(response);
                        bool isInt = int.TryParse(response, out int choice);
                        bool isInRange = choice < nextPossibleMoves.Count();

                        // act
                        if (isNotNullOrEmpty && isInt && isInRange)
                        {
                            nextMove = transformationCandidate.GetNextMoves(_response).ElementAt(choice);
                            break; // go to next stage
                        }
                        else
                        {
                            if (!isNotNullOrEmpty)
                                _communicationAgent.ErrorMessage($"Invalid input: Empty message received!");

                            else if (!isInt)
                                _communicationAgent.ErrorMessage($"Invalid input: Non-integer message received!");

                            else if (!isInRange)
                                _communicationAgent.ErrorMessage($"Invalid input: Message out of range received!");
                            else
                                _communicationAgent.ErrorMessage($"Invalid input, you must choose from the options given above. Please try again.");

                            _communicationAgent.Indent();
                        }
                    }

                    if (transformationCandidate.HasArguments && queryItems.Any())
                    {
                        // Get all possible transformations for the transformation
                        var nextPossibleArguments = transformationCandidate.GetArguments();
                        nextMoves = _communicationAgent.CreateNextQuestion($"---> {transformationCandidate.GetArgumentsInstructions()}", nextPossibleArguments);

                        // loop until getting satisfying answer
                        while (queryItems.Any())
                        {
                            // obtain the message
                            queryItems.RemoveAt(0);

                            responseQueryModel.NextMoves = nextMoves;

                            nextQueryItem = queryItems.FirstOrDefault();
                            index = responseQueryModel.NextMoves.ToList().IndexOf(nextQueryItem);

                            var response = _communicationAgent.GetResponse(querySoFar, nextQueryItem, index);
                            totalStepsMade++;
                            responseQueryModel.AddBotSuggestion(response);

                            // check the message
                            bool isNotNullOrEmpty = !string.IsNullOrEmpty(response);
                            bool isInt = int.TryParse(response, out int choice);
                            bool isInRange = choice < nextPossibleArguments.Count();

                            // act
                            if (isNotNullOrEmpty && isInt && isInRange)
                            {
                                firstArgument = transformationCandidate.GetArgumentAt(choice);
                                break; // go to next stage
                            }
                            else
                            {
                                if (!isNotNullOrEmpty)
                                    _communicationAgent.ErrorMessage($"Invalid input: Empty message received!");

                                else if (!isInt)
                                    _communicationAgent.ErrorMessage($"Invalid input: Non-integer message received!");

                                else if (!isInRange)
                                    _communicationAgent.ErrorMessage($"Invalid input: Message out of range received!");
                                else
                                    _communicationAgent.ErrorMessage($"Invalid input, you must choose from the options given above. Please try again.");

                                _communicationAgent.Indent();
                            }
                        }

                        if (transformationCandidate.HasFollowingHumanArguments && queryItems.Any())
                        {
                            _communicationAgent.CreateNextQuestion(transformationCandidate.GetFollowingHumanArgumentsInstructions());

                            // loop until getting satisfying answer
                            while (queryItems.Any())
                            {
                                // obtain the message
                                queryItems.RemoveAt(0);

                                nextQueryItem = queryItems.FirstOrDefault();
                                index = responseQueryModel.NextMoves.ToList().IndexOf(nextQueryItem);

                                var response = _communicationAgent.GetResponse(querySoFar, nextQueryItem, index, true);
                                totalStepsMade++;
                                responseQueryModel.AddBotSuggestion(response, true);

                                // check the message
                                bool isNotNullOrEmpty = !string.IsNullOrEmpty(response);

                                // act
                                if (isNotNullOrEmpty)
                                {
                                    secondArgument = response;
                                    break; // go to next stage
                                }
                                else
                                {
                                    _communicationAgent.ErrorMessage($"Invalid input: Empty message received!");
                                    _communicationAgent.Indent();
                                }
                            }
                        }
                    }

                    if (transformationCandidate is not null && totalStepsMade == transformationCandidate.TotalStepsNeeded)
                    {
                        // build the transformation
                        generatedTransformation = TransformationFactory.BuildTransformation(transformationName, new string[] { nextMove, firstArgument, secondArgument });

                        // blocks building the transformation when the last argument is given by chatBot
                        if (queryItems.Any())
                            responseQueryModel.AddTransformation(generatedTransformation);

                        // rebuild the possible response
                        _response = generatedTransformation.Preprocess(_response);
                    }
                }
                catch (ArgumentException ex)
                {
                    _communicationAgent.ErrorMessage($"{ex.Message}. Please try again.");
                    _communicationAgent.Indent();
                }
                _communicationAgent.Indent();
            }

            if (_communicationAgent.Verbose)
                _communicationAgent.ShowConversationHistory();

            return responseQueryModel;
        }

    }
}
