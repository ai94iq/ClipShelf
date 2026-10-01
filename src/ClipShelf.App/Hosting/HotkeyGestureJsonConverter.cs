using System.Text.Json;
using System.Text.Json.Serialization;
using ClipShelf.Core.Input;

namespace ClipShelf.App.Hosting;

// settings.json keeps the shortcut readable: "Win+Shift+V".
public sealed class HotkeyGestureJsonConverter : JsonConverter<HotkeyGesture>
{
    public override HotkeyGesture Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        HotkeyText.TryParse(reader.GetString(), out var gesture) ? gesture! : AppSettings.DefaultHotkey;

    public override void Write(Utf8JsonWriter writer, HotkeyGesture value, JsonSerializerOptions options) =>
        writer.WriteStringValue(HotkeyText.Format(value));
}
