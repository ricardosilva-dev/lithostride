using System;
using UnityEngine;

namespace Lithostride.UI
{
    /// <summary>
    /// Visualizador de animações: cada estação toca uma sequência do pack
    /// (Kael, efeitos, linhas do boss nas duas versões, lado a lado para
    /// comparar). O painel pausa, anda quadro a quadro e liga a linha de base
    /// (pivô) e os limites de cada quadro na sobreposição de depuração.
    /// </summary>
    public sealed class AnimationViewer : MonoBehaviour
    {
        [Serializable]
        private sealed class Station
        {
            [SerializeField] private SpriteRenderer renderer;
            [SerializeField] private Sprite[] frames;
            [SerializeField] private float framesPerSecond;

            public Station(SpriteRenderer renderer, Sprite[] frames, float framesPerSecond)
            {
                this.renderer = renderer;
                this.frames = frames;
                this.framesPerSecond = framesPerSecond;
            }

            public SpriteRenderer Renderer => renderer;
            public Sprite[] Frames => frames;
            public float FramesPerSecond => framesPerSecond;
        }

        [SerializeField] private Station[] stations;

        private float time;
        private int step;

        public bool Paused { get; private set; }
        public bool ShowBaseline { get; private set; } = true;
        public bool ShowBounds { get; private set; }
        public int StationCount => stations == null ? 0 : stations.Length;

        public SpriteRenderer StationRenderer(int index)
        {
            return stations[index].Renderer;
        }

        public void SetStations(SpriteRenderer[] renderers, Sprite[][] sequences, float[] framesPerSecond)
        {
            stations = new Station[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                stations[i] = new Station(renderers[i], sequences[i], framesPerSecond[i]);
            }
        }

        public void TogglePause()
        {
            Paused = !Paused;
        }

        /// <summary>Um quadro para frente ou para trás em todas as estações (pausa junto).</summary>
        public void Step(int direction)
        {
            Paused = true;
            step += direction;
            Apply();
        }

        public void ToggleBaseline()
        {
            ShowBaseline = !ShowBaseline;
        }

        public void ToggleBounds()
        {
            ShowBounds = !ShowBounds;
        }

        private void Update()
        {
            if (!Paused)
            {
                time += Time.deltaTime;
            }

            Apply();
        }

        private void Apply()
        {
            if (stations == null)
            {
                return;
            }

            foreach (Station station in stations)
            {
                if (station.Renderer == null || station.Frames == null || station.Frames.Length == 0)
                {
                    continue;
                }

                int count = station.Frames.Length;
                int index = (int)(time * station.FramesPerSecond) + step;
                station.Renderer.sprite = station.Frames[((index % count) + count) % count];
            }
        }
    }
}
