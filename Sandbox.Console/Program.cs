using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SixLabors.ImageSharp;

var tokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel("gpt-4o");
var count = tokenizer.CountTokens("Hello World!");

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: "gpt-4o",
    apiKey: "0f17c9c11fe5496f86b64561fd41d70b",
    endpoint: "https://9seven-ai-useast2.openai.azure.com/"//,
    //modelId: "gpt-4o"
);
var kernel = kernelBuilder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var chat = new ChatHistory("You should describe images in JSON only.");

var img = Image.Load("/Users/vadymivanenko/Development/PolartechLab/Data/Remforms/Credit Cards.png");

var message = new ChatMessageContentItemCollection()
{
    new TextContent("Describe this image in JSON."),
    new ImageContent(img.ToBase64String(img.Metadata.DecodedImageFormat!))
};
chat.AddUserMessage(message);
var result = await chatCompletionService.GetChatMessageContentAsync(chat);
Console.WriteLine(result.Content);