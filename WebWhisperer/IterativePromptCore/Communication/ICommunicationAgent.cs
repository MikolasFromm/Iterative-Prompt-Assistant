using WebWhisperer.IterativePromptCore.Types.Enums;

namespace WebWhisperer.IterativePromptCore.Communication
{
    public interface ICommunicationAgent
    {
        /// <summary>
        /// Returns the current chatBot verbose setting.
        /// </summary>
        public bool Verbose { get; }

        /// <summary>
        /// Represents current class setting status
        /// </summary>
        public CommunicationAgentMode Mode { get; }

        /// <summary>
        /// Removes the current chat and creates a new one.
        /// </summary>
        public void FlushCurrentChat();

        /// <summary>
        /// Adds a user query to the chatBot. If no query is given, it prompts the user for input.
        /// </summary>
        /// <param name="userQuery"></param>
        public void AddUserQuery(string userQuery = null);

        /// <summary>
        /// Add (system) message to the conversation.
        /// </summary>
        /// <param name="message"></param>
        public string InsertSystemMessage(string message);

        /// <summary>
        /// Adds user input to the system conversation. Relevant only for AIBot mode.
        /// </summary>
        /// <param name="message"></param>
        public string InsertUserMessage(string message);

        /// <summary>
        /// Default Error message "stream". Verbose independent
        /// </summary>
        /// <param name="message"></param>
        public string ErrorMessage(string message);

        /// <summary>
        /// Get response to the given query. If userMode set, content from <see cref="Console"/> will be given.
        /// </summary>
        /// <returns></returns>
        public string GetResponse(string querySoFar = null, string nextMove = null, int nextMoveIndex = -1, bool isUserInputExpected = false);

        /// <summary>
        /// Show the whole conversation history with chatbot.
        /// </summary>
        public void ShowConversationHistory();

        /// <summary>
        /// New-line indenting for more readable output. Relevant only for VERBOSE mode.
        /// </summary>
        public void Indent();

        /// <summary>
        /// Creates a next question and saves it until the AI Bot is asked to produce response.
        /// </summary>
        /// <param name="question"></param>
        /// <param name="possibleChoices"></param>
        /// <returns></returns>
        public IEnumerable<string> CreateNextQuestion(string question, IEnumerable<string> possibleChoices = null);
    }
}
