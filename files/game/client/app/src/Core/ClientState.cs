using System;
using System.Collections.Generic;

namespace Argentum.Client.Core;

public enum ClientState
{
    Boot,
    Connecting,
    Account,
    Characters,
    World,
    Closed,
}

public sealed class ClientStateMachine
{
    private static readonly IReadOnlyDictionary<ClientState, HashSet<ClientState>> Allowed =
        new Dictionary<ClientState, HashSet<ClientState>>
        {
            [ClientState.Boot] = [ClientState.Connecting, ClientState.Closed],
            [ClientState.Connecting] = [ClientState.Account, ClientState.Closed],
            [ClientState.Account] = [ClientState.Characters, ClientState.Account, ClientState.Closed],
            [ClientState.Characters] = [ClientState.World, ClientState.Account, ClientState.Closed],
            [ClientState.World] = [ClientState.Characters, ClientState.Closed],
            [ClientState.Closed] = [],
        };

    public ClientState Current { get; private set; } = ClientState.Boot;

    public bool CanTransition(ClientState next) => Allowed[Current].Contains(next);

    public void Transition(ClientState next)
    {
        if (!CanTransition(next))
        {
            throw new InvalidOperationException($"Invalid client transition: {Current} -> {next}");
        }
        Current = next;
    }
}
