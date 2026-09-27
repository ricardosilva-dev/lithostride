using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Lithostride.Core;
using Lithostride.EditorTools;
using Lithostride.Player;
using Lithostride.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Lithostride.Tests
{
    /// <summary>
    /// Kael sobre um terreno de tiles, com a física 2D avançada à mão
    /// (<see cref="Physics2D.Simulate(float)"/>) na mesma ordem do laço da
    /// Unity: em cada quadro, zero ou mais passos fixos (FixedUpdate dos
    /// scripts e depois a simulação) e só então o Update, que lê a entrada.
    /// O corpo vem do mesmo construtor da cena (<see cref="PackSceneBuilder.AddKaelBody"/>);
    /// o terreno usa a mesma montagem de colisão do jogo (TilemapCollider2D
    /// em Merge + CompositeCollider2D). A entrada é posta direto no
    /// <see cref="GameInput"/>, como ficaria depois de ler o teclado.
    /// </summary>
    public sealed class KaelPhysicsHarness : IDisposable
    {
        public readonly GameObject Root;
        public readonly Rigidbody2D Body;
        public readonly CapsuleCollider2D Capsule;
        public readonly PlayerController Controller;
        public readonly GameInput Input;
        public readonly Tilemap Ground;

        public readonly StringBuilder Log = new StringBuilder();
        public int Tick { get; private set; }
        public float Time { get; private set; }
        public int LandedCount { get; private set; }
        public int JumpedCount { get; private set; }

        /// <summary>Normal do chão lida pelo controle no último passo.</summary>
        public Vector2 GroundNormal => (Vector2)typeof(PlayerController).GetField("groundNormal", Any).GetValue(Controller);

        /// <summary>Chamado depois de cada passo fixo (medidas do teste).</summary>
        public Action<KaelPhysicsHarness> AfterFixedStep;

        private readonly GameObject groundRoot;
        private readonly Tile solidTile;
        private readonly SimulationMode2D previousMode;
        private readonly float fixedDelta;
        private float accumulator;
        private bool logEnabled;

        private static readonly BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public KaelPhysicsHarness(Vector2 feet, bool log)
        {
            previousMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
            fixedDelta = UnityEngine.Time.fixedDeltaTime;
            logEnabled = log;

            groundRoot = new GameObject("Terreno de teste");
            Grid grid = groundRoot.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            GameObject section = new GameObject("Sólido");
            section.transform.SetParent(groundRoot.transform, false);
            Ground = section.AddComponent<Tilemap>();
            Rigidbody2D staticBody = section.AddComponent<Rigidbody2D>();
            staticBody.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D tilemapCollider = section.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D composite = section.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

            solidTile = ScriptableObject.CreateInstance<Tile>();
            solidTile.colliderType = Tile.ColliderType.Grid;

            Root = new GameObject("Kael (teste)");
            Root.transform.position = feet;
            Capsule = PackSceneBuilder.AddKaelBody(Root, out Body);
            Body.interpolation = RigidbodyInterpolation2D.None;

            GameObject system = new GameObject("Entrada (teste)");
            Input = system.AddComponent<GameInput>();
            Controller = Root.AddComponent<PlayerController>();
            Controller.Configure(Body, Capsule, Input);
            Invoke(Controller, "Awake");
            Controller.Landed += _ => LandedCount++;
            Controller.Jumped += () => JumpedCount++;

            if (logEnabled)
            {
                Log.AppendLine("tick\tt\tpulo_apertado\tpulo_seguro\tbuffer\tcoyote\tno_ar\tsubindo\tno_chao\tcontatos\tnormal_y\tvy\ty\tdy_fixed\tevento");
            }
        }

        /// <summary>Preenche as células [x0..x1] x [y0..y1] como sólidas.</summary>
        public void Fill(int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    Ground.SetTile(new Vector3Int(x, y, 0), solidTile);
                }
            }
        }

        /// <summary>
        /// Rampa de 45 graus na célula, com o tile do próprio jogo (paleta do
        /// terreno): a forma física vem do sprite derivado pelo pipeline. Um
        /// sprite criado em código não aceita forma física própria no editor.
        /// </summary>
        public void Slope(int x, int y, bool upRight)
        {
            TerrainPalette palette = AssetDatabase.LoadAssetAtPath<TerrainPalette>(PackPipeline.PalettePath);
            if (palette == null || palette.MaterialCount == 0)
            {
                throw new InvalidOperationException("Paleta do terreno ausente: rode o pipeline do pack antes deste teste.");
            }

            TileBase tile = palette.Material(1).TileFor(upRight ? TerrainShape.SlopeUpRight : TerrainShape.SlopeUpLeft);
            Ground.SetTile(new Vector3Int(x, y, 0), tile);
        }

        public void Clear(int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    Ground.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }

        /// <summary>Atualiza a colisão depois de mexer nos tiles.</summary>
        public void Commit()
        {
            Ground.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            Ground.GetComponent<CompositeCollider2D>().GenerateGeometry();
            Physics2D.SyncTransforms();
        }

        /// <summary>
        /// Um quadro de duração <paramref name="dt"/>: passos fixos acumulados
        /// e depois o Update com a entrada deste quadro.
        /// </summary>
        public void Frame(float dt, float move, bool jumpPressed, bool jumpHeld, bool run = false)
        {
            accumulator += dt;
            while (accumulator >= fixedDelta - 1e-6f)
            {
                accumulator -= fixedDelta;
                float yBefore = Body.position.y;
                Invoke(Controller, "FixedUpdate");
                float yAfterScript = Body.position.y;
                Physics2D.Simulate(fixedDelta);
                Tick++;
                Time += fixedDelta;
                if (logEnabled)
                {
                    LogTick(yAfterScript - yBefore);
                }

                AfterFixedStep?.Invoke(this);
            }

            Set(Input, "MoveAxis", move);
            Set(Input, "RunHeld", run);
            Set(Input, "JumpPressed", jumpPressed);
            Set(Input, "JumpHeld", jumpHeld);
            Invoke(Controller, "Update");
            if (logEnabled && jumpPressed)
            {
                Log.AppendLine("--\t" + Time.ToString("0.000", CultureInfo.InvariantCulture) + "\tUpdate leu PULO APERTADO (buffer armado)");
            }
        }

        private void LogTick(float scriptDy)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            ContactPoint2D[] contacts = new ContactPoint2D[8];
            int count = Body.GetContacts(contacts);
            float normalY = count > 0 ? contacts[0].normal.y : 0f;
            string evento = Mathf.Abs(scriptDy) > 1e-4f ? (scriptDy > 0f ? "AUTODEGRAU" : "SNAP") : "";
            Log.Append(Tick).Append('\t').Append(Time.ToString("0.000", inv)).Append('\t')
                .Append(Input.JumpPressed ? 1 : 0).Append('\t').Append(Input.JumpHeld ? 1 : 0).Append('\t')
                .Append(Field(Controller, "jumpBufferTimer").ToString("0.000", inv)).Append('\t')
                .Append(Field(Controller, "coyoteTimer").ToString("0.000", inv)).Append('\t')
                .Append(Field(Controller, "airTime").ToString("0.000", inv)).Append('\t')
                .Append(Field(Controller, "jumpRising")).Append('\t')
                .Append(Controller.IsGrounded ? 1 : 0).Append('\t').Append(count).Append('\t')
                .Append(normalY.ToString("0.00", inv)).Append('\t')
                .Append(Body.linearVelocity.y.ToString("0.00", inv)).Append('\t')
                .Append(Body.position.y.ToString("0.000", inv)).Append('\t')
                .Append(scriptDy.ToString("0.000", inv)).Append('\t').Append(evento).AppendLine();
        }

        public void Dispose()
        {
            Object.DestroyImmediate(Root);
            Object.DestroyImmediate(Input.gameObject);
            Object.DestroyImmediate(groundRoot);
            Object.DestroyImmediate(solidTile);
            Physics2D.simulationMode = previousMode;
        }

        // ------------------------------------------------------------------ reflexão (campos privados do controle)

        public static void Invoke(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, Any);
            if (info == null)
            {
                throw new MissingMethodException(target.GetType().Name, method);
            }

            info.Invoke(target, null);
        }

        public static float Field(object target, string name)
        {
            FieldInfo info = target.GetType().GetField(name, Any);
            return info == null ? float.NaN : Convert.ToSingle(info.GetValue(target), CultureInfo.InvariantCulture);
        }

        private static void Set(object target, string property, object value)
        {
            PropertyInfo info = target.GetType().GetProperty(property, Any);
            info.GetSetMethod(true).Invoke(target, new[] { value });
        }
    }
}
