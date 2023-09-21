using System.Reactive.Linq;
using JetBrains.Annotations;
using NexusMods.Common.ProtocolRegistration;
using NexusMods.DataModel.Abstractions;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// Component for handling login and logout from the Nexus Mods
/// </summary>
[PublicAPI]
public class LoginManager
{
    private readonly OAuth _oauth;
    private readonly IDataStore _dataStore;
    private readonly IProtocolRegistration _protocolRegistration;
    private readonly Client _client;
    private readonly IAuthenticatingMessageFactory _msgFactory;

    /// <summary>
    /// Allows you to subscribe to notifications of when the user information changes.
    /// </summary>
    public IObservable<UserInfo?> UserInfo { get; }

    /// <summary>
    /// True if the user is logged in
    /// </summary>
    public IObservable<bool> IsLoggedIn => UserInfo.Select(info => info is not null);

    /// <summary>
    /// True if the user is logged in and is a premium member
    /// </summary>
    public IObservable<bool> IsPremium => UserInfo.Select(info => info?.IsPremium ?? false);

    /// <summary>
    /// The user's avatar
    /// </summary>
    public IObservable<Uri?> Avatar => UserInfo.Select(info => info?.Avatar);

    /// <summary/>
    /// <param name="client">Nexus API client.</param>
    /// <param name="msgFactory">Used to check authentication status and ensure verified.</param>
    /// <param name="oauth">Helper class to deal with authentication messages.</param>
    /// <param name="dataStore">Used for storing information about the current login session.</param>
    /// <param name="protocolRegistration">Used to register NXM protocol.</param>
    public LoginManager(Client client,
        IAuthenticatingMessageFactory msgFactory,
        OAuth oauth,
        IDataStore dataStore,
        IProtocolRegistration protocolRegistration)
    {
        _oauth = oauth;
        _msgFactory = msgFactory;
        _client = client;
        _dataStore = dataStore;
        _protocolRegistration = protocolRegistration;

        UserInfo = _dataStore.IdChanges
            // NOTE(err120): Since id's don't change on startup, we can insert
            // a fake change at the start of the observable chain. This will only
            // run once at startup and notify the subscribers.
            .Merge(Observable.Return(JWTTokenEntity.StoreId))
            .Where(id => id.Equals(JWTTokenEntity.StoreId))
            .SelectMany(async _ => await Verify(CancellationToken.None));
    }

    private async Task<UserInfo?> Verify(CancellationToken cancellationToken)
    {
        if (await _msgFactory.IsAuthenticated())
            return await _msgFactory.Verify(_client, cancellationToken);
        return null;
    }

    /// <summary>
    /// Show a browser and log into Nexus Mods
    /// </summary>
    /// <param name="token"></param>
    public async Task LoginAsync(CancellationToken token = default)
    {
        // temporary but if we want oauth to work we _have_ to be registered as the nxm handler
        await _protocolRegistration.RegisterSelf("nxm");

        var jwtToken = await _oauth.AuthorizeRequest(token);
        var createdAt = DateTimeOffset.FromUnixTimeSeconds(jwtToken.CreatedAt);
        var expiresIn = TimeSpan.FromSeconds(jwtToken.ExpiresIn);
        var expiresAt = createdAt + expiresIn;

        _dataStore.Put(JWTTokenEntity.StoreId, new JWTTokenEntity
        {
            RefreshToken = jwtToken.RefreshToken,
            AccessToken = jwtToken.AccessToken,
            ExpiresAt = expiresAt
        });
    }

    /// <summary>
    ///  Log out of Nexus Mods
    /// </summary>
    public Task Logout()
    {
        _dataStore.Delete(JWTTokenEntity.StoreId);
        return Task.CompletedTask;
    }
}
