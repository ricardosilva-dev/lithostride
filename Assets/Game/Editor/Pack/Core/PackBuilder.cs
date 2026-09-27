using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lithostride.EditorTools
{
    /// <summary>Informação de um arquivo de origem para o relatório de cobertura.</summary>
    public sealed class PackSourceInfo
    {
        public string Path;
        public string Sha256;
        public int Width;
        public int Height;
    }

    /// <summary>
    /// Gera todos os derivados do pack a partir das fontes (sem Unity): o
    /// editor escreve o resultado em Assets e a bancada de testes, numa pasta
    /// temporária. Também escreve o manifesto legível (JSON).
    /// </summary>
    public static class PackBuilder
    {
        /// <summary>Todos os PNG do pack que o processo precisa, na ordem do inventário.</summary>
        public static readonly string[] RequiredSources =
        {
            PackSpec.KaelMaster, PackSpec.KaelIdle, PackSpec.KaelWalk, PackSpec.KaelRun, PackSpec.KaelJump,
            PackSpec.Blocks1, PackSpec.Blocks2, PackSpec.Blocks3,
            PackSpec.TreeSheets[0], PackSpec.TreeSheets[1], PackSpec.TreeSheets[2], PackSpec.TreeSheets[3],
            PackSpec.GroundItems,
            PackSpec.Backgrounds[0], PackSpec.Backgrounds[1], PackSpec.Backgrounds[2],
            PackSpec.Sword, PackSpec.Effects[0], PackSpec.Effects[1], PackSpec.Effects[2], PackSpec.Effects[3], PackSpec.Effects[4],
            PackSpec.BossSheets[0], PackSpec.BossSheets[1], PackSpec.HealthBar,
            HarmoniaSpec.TerraMineral, HarmoniaSpec.TerraRaizes, HarmoniaSpec.PedraNatural, HarmoniaSpec.GramaBordas
        };

        public static List<DerivedSheet> Build(Func<string, PixelImage> load, List<string> notes)
        {
            List<DerivedSheet> sheets = new List<DerivedSheet>();
            sheets.AddRange(KaelArt.Build(load, notes));
            sheets.AddRange(TerrainArt.Build(load, notes));
            sheets.AddRange(VegetationArt.Build(load, notes));
            sheets.AddRange(EffectsArt.Build(load, notes));
            sheets.AddRange(BossArt.Build(load, notes));
            sheets.AddRange(InterfaceArt.Build(load, notes));
            return sheets;
        }

        /// <summary>Manifesto: fontes (com hash e uso), folhas derivadas e cada sprite.</summary>
        public static string Manifest(List<DerivedSheet> sheets, List<PackSourceInfo> sources, List<string> notes)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"descricao\": \"Manifesto do Pack completo: gerado por Lithostride/Pack completo. Retângulos em pixels; " +
                      "'rect_topo' tem origem no canto de cima (como a imagem), 'rect_unity' no canto de baixo (Unity). " +
                      "Pivô normalizado com origem embaixo à esquerda. Âncoras em pixels relativos ao pivô, y para cima.\",\n");
            sb.Append("  \"escala\": {\"px_logicos_por_unidade\": ").Append(PackSpec.LogicalPixelsPerUnit)
                .Append(", \"ambiente_ppu\": ").Append(PackSpec.EnvironmentPixelsPerUnit)
                .Append(", \"boss_ppu\": ").Append(PackSpec.BossPixelsPerUnit)
                .Append(", \"texels_por_bloco\": ").Append(PackSpec.TileTexels).Append("},\n");

            sb.Append("  \"fontes\": [\n");
            for (int i = 0; i < sources.Count; i++)
            {
                PackSourceInfo s = sources[i];
                List<string> users = new List<string>();
                int count = 0;
                string fileName = s.Path.Substring(s.Path.LastIndexOf('/') + 1);
                foreach (DerivedSheet sheet in sheets)
                {
                    int inSheet = 0;
                    foreach (DerivedSprite sprite in sheet.Sprites)
                    {
                        if (sprite.Source != null && (sprite.Source.Contains(s.Path) || sprite.Source.Contains(fileName)))
                        {
                            inSheet++;
                        }
                    }

                    if (sheet.SheetSources.Contains(s.Path))
                    {
                        users.Add(sheet.AssetPath + " (folha inteira)");
                        count++;
                    }

                    if (inSheet > 0)
                    {
                        users.Add(sheet.AssetPath + " (" + inSheet + ")");
                        count += inSheet;
                    }
                }

                string alias = AliasOf(s.Path, sources);
                sb.Append("    {\"arquivo\": ").Append(Q(s.Path)).Append(", \"sha256\": ").Append(Q(s.Sha256))
                    .Append(", \"tamanho\": [").Append(s.Width).Append(", ").Append(s.Height).Append("]")
                    .Append(", \"recortes\": ").Append(count)
                    .Append(", \"alias_de\": ").Append(Q(alias))
                    .Append(", \"derivados\": ").Append(List(users)).Append("}")
                    .Append(i < sources.Count - 1 ? ",\n" : "\n");
            }

            sb.Append("  ],\n");
            sb.Append("  \"notas\": ").Append(List(notes)).Append(",\n");
            sb.Append("  \"folhas\": [\n");
            for (int i = 0; i < sheets.Count; i++)
            {
                DerivedSheet sheet = sheets[i];
                sb.Append("    {\"asset\": ").Append(Q(sheet.AssetPath)).Append(", \"categoria\": ").Append(Q(sheet.Category))
                    .Append(", \"tamanho\": [").Append(sheet.Image.Width).Append(", ").Append(sheet.Image.Height).Append("]")
                    .Append(", \"ppu\": ").Append(sheet.PixelsPerUnit)
                    .Append(", \"filtro\": ").Append(Q(sheet.PointFilter ? "point" : "bilinear"))
                    .Append(", \"fontes_da_folha\": ").Append(List(sheet.SheetSources))
                    .Append(", \"sprites\": [\n");
                for (int j = 0; j < sheet.Sprites.Count; j++)
                {
                    DerivedSprite sp = sheet.Sprites[j];
                    RectI r = sp.Rect;
                    sb.Append("      {\"nome\": ").Append(Q(sp.Name))
                        .Append(", \"rect_topo\": [").Append(r.X).Append(", ").Append(r.Y).Append(", ").Append(r.W).Append(", ").Append(r.H).Append("]")
                        .Append(", \"rect_unity\": [").Append(r.X).Append(", ").Append(sheet.Image.Height - r.Y - r.H).Append(", ").Append(r.W).Append(", ").Append(r.H).Append("]")
                        .Append(", \"pivo\": [").Append(sp.PivotX.ToString("0.####", inv)).Append(", ").Append(sp.PivotY.ToString("0.####", inv)).Append("]")
                        .Append(", \"origem\": ").Append(Q(sp.Source))
                        .Append(", \"rect_origem_topo\": [").Append(sp.SourceRect.X).Append(", ").Append(sp.SourceRect.Y).Append(", ")
                        .Append(sp.SourceRect.W).Append(", ").Append(sp.SourceRect.H).Append("]")
                        .Append(", \"sequencia\": ").Append(Q(sp.Sequence)).Append(", \"ordem\": ").Append(sp.Order)
                        .Append(", \"papel\": ").Append(Q(sp.Role))
                        .Append(", \"ajustes\": ").Append(Q(sp.Adjustments));
                    if (sp.Anchors.Count > 0)
                    {
                        sb.Append(", \"ancoras\": {");
                        int k = 0;
                        foreach (KeyValuePair<string, float[]> anchor in sp.Anchors)
                        {
                            sb.Append(k++ > 0 ? ", " : "").Append(Q(anchor.Key)).Append(": [");
                            for (int v = 0; v < anchor.Value.Length; v++)
                            {
                                sb.Append(v > 0 ? ", " : "").Append(anchor.Value[v].ToString("0.##", inv));
                            }

                            sb.Append("]");
                        }

                        sb.Append("}");
                    }

                    if (sp.PhysicsShape != null)
                    {
                        sb.Append(", \"fisica\": [");
                        for (int p = 0; p < sp.PhysicsShape.Count; p++)
                        {
                            sb.Append(p > 0 ? ", " : "").Append("[");
                            float[] poly = sp.PhysicsShape[p];
                            for (int v = 0; v < poly.Length; v++)
                            {
                                sb.Append(v > 0 ? ", " : "").Append(poly[v].ToString("0.##", inv));
                            }

                            sb.Append("]");
                        }

                        sb.Append("]");
                    }

                    sb.Append("}").Append(j < sheet.Sprites.Count - 1 ? ",\n" : "\n");
                }

                sb.Append("    ]}").Append(i < sheets.Count - 1 ? ",\n" : "\n");
            }

            sb.Append("  ]\n}\n");
            return sb.ToString();
        }

        /// <summary>Arquivo com o mesmo hash usado no lugar (cópia idêntica), ou vazio.</summary>
        private static string AliasOf(string path, List<PackSourceInfo> sources)
        {
            PackSourceInfo self = null;
            foreach (PackSourceInfo s in sources)
            {
                if (s.Path == path) self = s;
            }

            if (self == null)
            {
                return "";
            }

            foreach (PackSourceInfo s in sources)
            {
                if (s.Path != path && s.Sha256 == self.Sha256 && Array.IndexOf(RequiredSources, s.Path) >= 0 &&
                    Array.IndexOf(RequiredSources, path) < 0)
                {
                    return s.Path;
                }
            }

            return "";
        }

        private static string List(List<string> items)
        {
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < items.Count; i++)
            {
                sb.Append(i > 0 ? ", " : "").Append(Q(items[i]));
            }

            return sb.Append("]").ToString();
        }

        private static string Q(string text)
        {
            if (text == null)
            {
                return "\"\"";
            }

            StringBuilder sb = new StringBuilder("\"");
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }

            return sb.Append("\"").ToString();
        }
    }
}
