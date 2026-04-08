using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Makapi.Messages;

internal sealed class SettingsChangedMessage : ValueChangedMessage<object?>
{
    public SettingsChangedMessage() : base(null)
    {
    }
}
