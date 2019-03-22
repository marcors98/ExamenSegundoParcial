using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Reproductor
{
    class Delay : ISampleProvider
    {
        private ISampleProvider fuente;
        private int offsetMS;
        public int OffsetMilisegundos { get
            {
                return offsetMS;
            }
            set
            {
                offsetMS = value;
                cantidadMuestrasOffset = (int)
                (((float)offsetMS / 1000.0f) *
                (float)fuente.WaveFormat.SampleRate);
            }
        }
        private int cantidadMuestrasOffset;

        private List<float> bufferDelay =
            new List<float>();
        private int tamañoBuffer;
        private int duracionBufferSegundos;
        private int cantidadMuestrasTranscurridas = 0;
        private int cantidadMuestrasBorradas = 0;
        public bool Activo { get; set; }
        public bool Inverso { get; set; }

        public WaveFormat WaveFormat
        {
            get
            {
                return fuente.WaveFormat;
            }
        }

        public Delay(ISampleProvider fuente)
        {
            this.fuente = fuente;
            OffsetMilisegundos = 500;
            cantidadMuestrasOffset = (int)
                (((float)OffsetMilisegundos / 1000.0f) * 
                (float)fuente.WaveFormat.SampleRate);
            duracionBufferSegundos = 10;
            tamañoBuffer =
                fuente.WaveFormat.SampleRate * duracionBufferSegundos;
            Activo = true;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            //Leemos las muestras de la señal fuente
            var read =
                fuente.Read(buffer, offset, count);
            //Calcular tiempo transcurrido
            float tiempoTranscurridoSegundos =
                (float)cantidadMuestrasTranscurridas / 
                (float)fuente.WaveFormat.SampleRate;
            float milisegundosTranscurridos =
                tiempoTranscurridoSegundos * 1000.0f;
            //Llenando el buffer
            for(int i=0; i < read; i++)
            {
                bufferDelay.Add(buffer[i + offset]);
            }
            //Eliminar excedentes del buffer
            if (bufferDelay.Count > tamañoBuffer)
            {
                int diferencia = 
                    bufferDelay.Count - tamañoBuffer;
                bufferDelay.RemoveRange(0, diferencia);
                cantidadMuestrasBorradas += diferencia;
            }
            //Aplicar el efecto
            if (Activo)
            {
                if (milisegundosTranscurridos > OffsetMilisegundos)
                {
                    if (Inverso)
                        for (int i = 0; i < read; i++)
                        {
                            buffer[offset + i] +=
                                bufferDelay[cantidadMuestrasTranscurridas - cantidadMuestrasBorradas + i - cantidadMuestrasOffset] * (-1);
                        }
                    else
                        for (int i = 0; i < read; i++)
                        {
                            buffer[offset + i] +=
                                bufferDelay[cantidadMuestrasTranscurridas -
                                cantidadMuestrasBorradas
                                + i - cantidadMuestrasOffset];
                        }
                }
            }


            cantidadMuestrasTranscurridas +=
                read;
            return read;
        }
    }
}
