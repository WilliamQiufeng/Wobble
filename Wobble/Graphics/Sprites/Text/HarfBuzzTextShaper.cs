using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FontStashSharp.Interfaces;
using HarfBuzzSharp;

namespace Wobble.Graphics.Sprites.Text
{
    internal class HarfBuzzTextShaper : ITextShaper
    {
        private static readonly Feature TabularFiguresFeature = Feature.Parse("tnum=1");
        private static readonly Tag WeightAxisTag = new Tag('w', 'g', 'h', 't');
        private static readonly Tag HorizontalHeaderTableTag = new Tag('h', 'h', 'e', 'a');

        private readonly FreeTypeFontLoader _fontLoader;
        private readonly Dictionary<int, RegisteredFont> _fonts = new Dictionary<int, RegisteredFont>();
        private int _nextFontId;

        public HarfBuzzTextShaper(FreeTypeFontLoader fontLoader)
        {
            _fontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
        }

        public int RegisterTtfFont(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var settings = _fontLoader.GetSettings(data);
            var id = _nextFontId++;
            _fonts[id] = new RegisteredFont(data, settings);
            return id;
        }

        public void RemoveFont(int id)
        {
            RegisteredFont font;

            if (!_fonts.TryGetValue(id, out font))
                return;

            font.Dispose();
            _fonts.Remove(id);
        }

        public ShapedText Shape(string text, float fontSize, ITextShapingInfoProvider infoProvider)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (infoProvider == null)
                throw new ArgumentNullException(nameof(infoProvider));

            var shapedGlyphs = new List<ShapedGlyph>();
            var index = 0;

            while (index < text.Length)
            {
                var codepoint = GetCodepoint(text, index, out var codepointLength);
                var fontSourceId = infoProvider.GetFontSourceId(codepoint);

                if (!fontSourceId.HasValue)
                {
                    index += codepointLength;
                    continue;
                }

                var runStart = index;
                var runFontSourceId = fontSourceId.Value;
                index += codepointLength;

                while (index < text.Length)
                {
                    codepoint = GetCodepoint(text, index, out codepointLength);
                    fontSourceId = infoProvider.GetFontSourceId(codepoint);

                    if (!fontSourceId.HasValue || fontSourceId.Value != runFontSourceId)
                        break;

                    index += codepointLength;
                }

                ShapeRun(text, runStart, index - runStart, runFontSourceId, fontSize, infoProvider, shapedGlyphs);
            }

            return new ShapedText
            {
                Glyphs = shapedGlyphs.ToArray(),
                OriginalText = text,
                FontSize = fontSize
            };
        }

        private void ShapeRun(string text, int start, int length, int fontSourceId, float fontSize,
            ITextShapingInfoProvider infoProvider, List<ShapedGlyph> shapedGlyphs)
        {
            var textShaperFontId = infoProvider.GetTextShaperFontId(fontSourceId);
            RegisteredFont registeredFont;

            if (!_fonts.TryGetValue(textShaperFontId, out registeredFont))
                return;

            using (var buffer = new HarfBuzzSharp.Buffer())
            {
                buffer.AddUtf16(text, start, length);
                buffer.GuessSegmentProperties();

                registeredFont.Font.Shape(buffer, registeredFont.Features);

                var scale = infoProvider.CalculateScale(fontSourceId, fontSize);
                var infos = buffer.GetGlyphInfoSpan();
                var positions = buffer.GetGlyphPositionSpan();

                for (var i = 0; i < infos.Length; i++)
                {
                    var info = infos[i];
                    var position = positions[i];

                    shapedGlyphs.Add(new ShapedGlyph
                    {
                        GlyphId = (int)info.Codepoint,
                        Cluster = start + (int)info.Cluster,
                        FontSourceId = fontSourceId,
                        XAdvance = position.XAdvance * scale,
                        YAdvance = position.YAdvance * scale,
                        XOffset = position.XOffset * scale,
                        YOffset = -position.YOffset * scale
                    });
                }
            }
        }

        private static int GetCodepoint(string text, int index, out int length)
        {
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length
                && char.IsLowSurrogate(text[index + 1]))
            {
                length = 2;
                return char.ConvertToUtf32(text[index], text[index + 1]);
            }

            length = 1;
            return text[index];
        }

        private sealed class RegisteredFont : IDisposable
        {
            public Feature[] Features { get; }

            public Font Font { get; }

            private readonly GCHandle _dataHandle;
            private readonly Blob _blob;
            private readonly Face _face;

            public RegisteredFont(byte[] data, IndexedFontSettings settings)
            {
                _dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                _blob = new Blob(_dataHandle.AddrOfPinnedObject(), data.Length, MemoryMode.ReadOnly);
                _face = new Face(_blob, settings.Index);
                Font = new Font(_face);
                var scale = GetFontScale(_face);
                Font.SetScale(scale, scale);
                SetVariations(settings);
                Features = settings.EnableTabularNumbers
                    ? new[] { TabularFiguresFeature }
                    : Array.Empty<Feature>();
            }

            public void Dispose()
            {
                Font.Dispose();
                _face.Dispose();
                _blob.Dispose();

                if (_dataHandle.IsAllocated)
                    _dataHandle.Free();
            }

            private void SetVariations(IndexedFontSettings settings)
            {
                if (settings.Weight == FontWeight.Regular || !_face.HasVariationData
                    || !_face.TryFindVariationAxis(WeightAxisTag, out var axis))
                    return;

                var weight = Math.Max(axis.MinValue, Math.Min(axis.MaxValue, settings.Weight));

                Font.SetVariations(new[]
                {
                    new Variation
                    {
                        Tag = WeightAxisTag,
                        Value = weight
                    }
                });
            }

            private static int GetFontScale(Face face)
            {
                using (var table = face.ReferenceTable(HorizontalHeaderTableTag))
                {
                    if (table.Length >= 8)
                    {
                        var data = table.AsSpan();
                        var ascender = ReadInt16(data, 4);
                        var descender = ReadInt16(data, 6);
                        var height = ascender - descender;

                        if (height > 0)
                            return height;
                    }
                }

                return face.UnitsPerEm;
            }

            private static short ReadInt16(Span<byte> data, int offset)
            {
                return unchecked((short)((data[offset] << 8) | data[offset + 1]));
            }
        }
    }
}
