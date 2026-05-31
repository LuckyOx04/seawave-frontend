using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SeawaveApp.Models;

public class PlaylistChangedMessage(Playlist? value) : ValueChangedMessage<Playlist?>(value);