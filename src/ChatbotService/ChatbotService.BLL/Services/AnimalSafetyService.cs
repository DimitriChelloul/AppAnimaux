namespace ChatbotService.BLL.Services;

public sealed class AnimalSafetyService
{
    private readonly AnimalSafetyClassifier _classifier;

    public AnimalSafetyService(AnimalSafetyClassifier classifier)
    {
        _classifier = classifier;
    }

    public bool RequiresVeterinaryAttention(string message) => _classifier.RequiresVeterinaryAttention(message);
}
