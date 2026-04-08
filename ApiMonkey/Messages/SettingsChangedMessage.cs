using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ApiMonkey.Messages;

internal sealed class SettingsChangedMessage : ValueChangedMessage<object?>
{
    public SettingsChangedMessage() : base(null)
    {
    }
}
