using System.Data;
using Shared.Persistence.Abstractions;

namespace ChatbotService.DAL.Db;

public sealed class ChatbotDbConnectionFactory
{
    private readonly IDbConnectionFactory _inner;

    public ChatbotDbConnectionFactory(IDbConnectionFactory inner)
    {
        _inner = inner;
    }

    public IDbConnection Create() => _inner.Create();
}
