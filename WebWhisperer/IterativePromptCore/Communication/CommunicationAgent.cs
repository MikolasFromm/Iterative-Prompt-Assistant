#define BRIEF_START

using OpenAI_API;
using OpenAI_API.Models;
using WebWhisperer.IterativePromptCore.Types;

namespace WebWhisperer.IterativePromptCore.Communication
{
    public class CommunicationAgent : ICommunicationAgent
    {
        private CommunicationAgentMode _mode;

        private OpenAIAPI? _api;

        private bool _verbose;

        private OpenAI_API.Chat.Conversation? _chat;

        private string _userInputQuery;

        private string _nextComingQuestion;

        /// <summary>
        /// Introduces a role to the chatBot with SystemMessages.
        /// </summary>
        private void BotIntroduction()
        {

#if WORD_MATCHING
            _chat.AppendSystemMessage("You are an assistant who should translate given user input into a query request.");
            _chat.AppendSystemMessage("You always get instructions and options from which you can choose.");
            _chat.AppendSystemMessage("You must use at most 2 words only from the selection given."); // default PerformQuery
            _chat.AppendSystemMessage("But usually use just one word."); // default PerformQuery
            _chat.AppendSystemMessage("You can not use any other words than the ones given from the user input."); // default PerformQuery
#endif

#if INDEXING
            _chat.AppendSystemMessage("You write your choice as a number from the [] brackets. Dont write anything else!"); // extension PerformQueryWithIndices
            _chat.AppendSystemMessage("Only when you are asked to write a word, you can write any word you want."); // extension PerformQueryWithIndices

            // TODO: add more instructions in the resulting format

            _chat.AppendUserInput("Example: You might choose from the following; [0] SortBy, [1] FilterBy and so on. To pick SortBy, you should only write \"0\" and nothing else.");
#endif

#if BETTER_INDEXING
            _chat.AppendSystemMessage("You are an external API which translates user input into a query request."); // extension PerformQueryWithIndices
            _chat.AppendSystemMessage("You sequentially build the query from left to right. You always get all next possible actions and you must always choose one."); // extension PerformQueryWithIndices
            _chat.AppendSystemMessage("You always get a list of all available transformations or its arguments. When one transformation is finished, you get all possible next transformations."); // extension PerformQueryWithIndices
            _chat.AppendSystemMessage("If you feel you have finished the query, choose the \"[0] Empty\" transformation."); // extension PerformQueryWithIndices
            _chat.AppendSystemMessage("You must write only the numbers from the brackets. Dont write anything else!"); // extension PerformQueryWithIndices
            _chat.AppendSystemMessage("If the answer should not be a number, write the whole appropriate word."); // extension PerformQueryWithIndices

            _chat.AppendSystemMessage(string.Empty);

            // extension PerformQueryWithIndices example
            _chat.AppendSystemMessage("Let me show you an example: \n" +
                "Sort the people by their names and filter out those born outside Prague. \n" +
                "Possible next moves are: \n" +
                "[0] SortBy, [1] FilterBy, [2] GroupBy, [3] DropColumn \n" +
                "\n" +
                "YOU SHOULD ANSWER: \n" +
                "0");
#endif

#if BRIEF_START
            _chat.AppendSystemMessage("Build a trasformation query sequentially from left to right. You always get all next possible actions and you must always choose one.");

            _chat.AppendSystemMessage(
                "Let me show you an example: \n" +
                "Sort the people by their names and filter out those born outside Prague. \n" +
                "First step would be built like: \n" +
                "> [0] SortBy, \n" +
                "> [1] FilterBy, \n" +
                "> [2] GroupBy, \n" +
                "> [3] DropColumn \n" +
                "Your answer: \n" +
                "0");

            _chat.AppendSystemMessage("Only answer with a number from the brackets. If the answer should not be a number, write the whole appropriate word.");
#endif
        }

        public void AddUserQuery(string userQuery = null)
        {

            // creates new chat and flushes the old one
            CrateNewChat();

            // loads the userquery when empty
            if (string.IsNullOrEmpty(userQuery))
            {
                Console.WriteLine("Write your query: ");
                userQuery = Console.ReadLine();
            }

            _userInputQuery = userQuery;

            if (_verbose)
                Console.WriteLine($"User input: {userQuery}");
        }

        public void FlushCurrentChat()
        {
            CrateNewChat();
        }

        private void CrateNewChat()
        {
            if (_api is not null)
            {
                _chat = _api.Chat.CreateConversation();
                _chat.RequestParameters.TopP = 0.0;
                _chat.RequestParameters.Model = OpenAI_API.Models.Model.ChatGPTTurbo0301;
                BotIntroduction();
            }
        }

        public bool Verbose { get { return _verbose; } }

        /// <summary>
        /// Constructor for OpenAIAPIBot feature. Use when communication with bot required. <see cref="OpenAIAPI"/> should be already initialized with API-token and all related settings.
        /// Also initializes the chatBot with introduction and gives him the user query input obtained via <see cref="Console"/>.
        /// Creates new local <see cref="OpenAI_API.Chat.Conversation"/> instance.
        /// </summary>
        /// <param name="api">Initialized API for OpenAI bot.</param>
        /// <param name="verbose"></param>
        public CommunicationAgent(OpenAIAPI api, bool verbose)
        {
            _api = api;
            _mode = CommunicationAgentMode.AIBot;
            _verbose = verbose;
        }

        /// <summary>
        /// Constructor for user inputs. Will not provide any responses.
        /// </summary>
        /// <param name="verbose"></param>
        public CommunicationAgent(bool verbose)
        {
            _mode = CommunicationAgentMode.User;
            _verbose = verbose;
        }

        public CommunicationAgentMode Mode { get { return _mode; } }

        public string InsertSystemMessage(string message)
        {
            if (_verbose)
                Console.WriteLine(message);

            if (_mode == CommunicationAgentMode.AIBot || _mode == CommunicationAgentMode.AIBotWebWhisper)
                _chat.AppendSystemMessage(message);

            return message;
        }

        public string InsertUserMessage(string message)
        {
            if (_verbose)
                Console.WriteLine(message);

            if (_mode == CommunicationAgentMode.AIBot || _mode == CommunicationAgentMode.AIBotWebWhisper)
            {
                _chat.AppendUserInput(message);
            }

            return message;
        }

        public IEnumerable<string> CreateNextQuestion(string question, IEnumerable<string> possibleChoices = null)
        {

            if (_mode == CommunicationAgentMode.AIBot || _mode == CommunicationAgentMode.AIBotWebWhisper)
            {
                _nextComingQuestion = $"{question}\n";

                if (possibleChoices is not null)
                {
                    int i = 0;
                    foreach (var choice in possibleChoices)
                    {
                        _nextComingQuestion += $"> [{i++}] {choice}\n";
                    }
                }

                _nextComingQuestion += "\n";
            }

            return possibleChoices;
        }

        public string ErrorMessage(string message)
        {
            if (_mode == CommunicationAgentMode.User)
                Console.WriteLine($"ERROR: {message}");

            if (_mode == CommunicationAgentMode.AIBot || _mode == CommunicationAgentMode.AIBotWebWhisper)
                InsertUserMessage($"ERROR: {message}");

            return message;
        }

        public string GetResponse(string querySoFar = null, string nextMove = null, int nextMoveIndex = -1, bool isUserInputExpected = false)
        {
            if (_mode == CommunicationAgentMode.User)
            {
                string response = Console.ReadLine();

                if (_verbose)
                {
                    Console.WriteLine($"User response: {response}");
                }

                if (response is not null)
                    return response;
            }

            if (_mode == CommunicationAgentMode.AIBot || _mode == CommunicationAgentMode.AIBotWebWhisper)
            {

                string response = string.Empty;

                // when processing the next move
                if (string.IsNullOrEmpty(nextMove))
                {
                    // show bot the user input
                    _chat.AppendUserInput($"User initial input is: {_userInputQuery}");

                    // show bot the query so far
                    _chat.AppendUserInput($"The query build so far: {querySoFar}");

                    // show the possibilities
                    InsertUserMessage(_nextComingQuestion);

                    if (isUserInputExpected == false)
                        _chat.AppendUserInput("Answer the apropriate number!");

                    // get response from the chatbot
                    response = _chat.GetResponseFromChatbotAsync().Result;

                    // show the history
                    //ShowConversationHistory();
                }

                // when still processing the already given query
                else
                {
                    // show the possibilities
                    InsertUserMessage(_nextComingQuestion);

                    // insert the response to the AI chat to follow the conversation
                    if (isUserInputExpected)
                    {
                        _chat.AppendUserInput($"{nextMove}");
                        response = nextMove;
                    }
                    else
                    {
                        _chat.AppendUserInput($"{nextMoveIndex}");
                        response = nextMoveIndex.ToString();
                    }
                }

                if (_verbose)
                {
                    if (string.IsNullOrEmpty(nextMove))
                    {
                        Console.WriteLine($"Automatic response: {response}");
                    }
                    else
                    {
                        Console.WriteLine($"Bot response: {response}");
                    }
                }

                return response;
            }

            return string.Empty;
        }

        public void ShowConversationHistory()
        {
            if (_mode == CommunicationAgentMode.AIBot || _mode == CommunicationAgentMode.AIBotWebWhisper)
            {
                Console.WriteLine("----------");
                Console.WriteLine();
                Console.WriteLine();
                foreach (var message in _chat.Messages)
                {
                    Console.WriteLine($"{message.Role}: {message.Content}");
                }
            }
        }

        public void Indent()
        {
            if (_mode == CommunicationAgentMode.AIBot || _mode == CommunicationAgentMode.AIBotWebWhisper)
            {
                _chat.AppendUserInput(string.Empty);
            }


            if (_verbose)
                Console.WriteLine();
        }
    }
}
