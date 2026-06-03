using CommunityToolkit.Mvvm.Messaging.Messages;
using SeawaveApp.Models;

namespace SeawaveApp.Messages;

public class PlaylistChangedMessage(Playlist? value) : ValueChangedMessage<Playlist?>(value);