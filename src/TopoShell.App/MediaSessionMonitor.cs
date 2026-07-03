using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace TopoShell.App;

internal sealed class MediaSessionMonitor
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    public async Task<MediaSnapshot> ReadAsync()
    {
        try
        {
            _manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

            var session = _manager.GetCurrentSession();
            if (session is null)
            {
                return MediaSnapshot.Empty;
            }

            var playback = session.GetPlaybackInfo();
            var properties = await session.TryGetMediaPropertiesAsync();
            var artwork = await LoadThumbnailAsync(properties.Thumbnail);

            return new MediaSnapshot(
                properties.Title ?? string.Empty,
                properties.Artist ?? string.Empty,
                properties.AlbumTitle ?? string.Empty,
                artwork,
                playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
        }
        catch
        {
            return MediaSnapshot.Empty;
        }
    }

    private static async Task<ImageSource?> LoadThumbnailAsync(IRandomAccessStreamReference? thumbnail)
    {
        if (thumbnail is null)
        {
            return null;
        }

        try
        {
            var stream = await thumbnail.OpenReadAsync();
            try
            {
                var byteCount = checked((int)Math.Min(stream.Size, 8_000_000));
                if (byteCount == 0)
                {
                    return null;
                }

                using var reader = new DataReader(stream);
                await reader.LoadAsync((uint)byteCount);

                var bytes = new byte[byteCount];
                reader.ReadBytes(bytes);

                using var memory = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = memory;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            finally
            {
                stream.Dispose();
            }
        }
        catch
        {
            return null;
        }
    }
}

internal sealed record MediaSnapshot(
    string Title,
    string Artist,
    string Album,
    ImageSource? Artwork,
    bool IsPlaying)
{
    public static MediaSnapshot Empty { get; } = new(string.Empty, string.Empty, string.Empty, null, false);

    public bool HasSession =>
        !string.IsNullOrWhiteSpace(Title) ||
        !string.IsNullOrWhiteSpace(Artist) ||
        !string.IsNullOrWhiteSpace(Album);
}
