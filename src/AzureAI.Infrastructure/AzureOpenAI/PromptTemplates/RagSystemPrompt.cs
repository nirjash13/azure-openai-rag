namespace AzureAI.Infrastructure.AzureOpenAI.PromptTemplates;

/// <summary>Builds the system prompt used in RAG-augmented conversations.</summary>
public static class RagSystemPrompt
{
    /// <summary>
    /// Constructs the system prompt that instructs the model to answer only from
    /// the provided context window and optionally cite sources.
    /// </summary>
    /// <param name="contextWindow">Pre-assembled context text from retrieved document chunks.</param>
    /// <param name="includeCitations">When <see langword="true"/>, instructs the model to cite sources.</param>
    public static string Build(string contextWindow, bool includeCitations)
    {
        var citationInstruction = includeCitations
            ? "\nCite your sources using [Source: <document name>] notation after each claim."
            : string.Empty;

        return $"""
            You are a precise and helpful assistant. Answer the user's question using ONLY the information in the CONTEXT section below.
            If the context does not contain sufficient information, say so explicitly — do not invent or infer facts.
            Be concise and accurate. Use bullet points for multi-part answers.{citationInstruction}

            CONTEXT:
            {contextWindow}
            """;
    }
}
