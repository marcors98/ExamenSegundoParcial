﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using System.Windows.Threading;

namespace Reproductor
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AudioFileReader reader;
        //Nuestra comunicacion con la tarjeta de sonido
        WaveOutEvent output;

        DispatcherTimer timer;
        EfectoVolumen volume;
        FadeInOutSampleProvider fades;
        Delay delay;
        bool fadingOut = false;
        bool dragging = false;

        public MainWindow()
        {
            InitializeComponent();
            LlenarComboSalida();

            //Inicializar timer
            //Establecer tiempo entre ejecuciones
            //Establecer lo que se va a ejecutar
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;

            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (reader != null)
            {
                lblTiempoActual.Text =
                    reader.CurrentTime.ToString().Substring(0, 8);
                if (!dragging)
                {
                    sldReproduccion.Value =
                        reader.CurrentTime.TotalSeconds;
                }
                
            }
        }

        private void LlenarComboSalida()
        {
            cbSalida.Items.Clear();
            for(int i=0; i < WaveOut.DeviceCount; i++)
            {
                WaveOutCapabilities capacidades =
                    WaveOut.GetCapabilities(i);
                cbSalida.Items.Add(capacidades.ProductName);
            }
            cbSalida.SelectedIndex = 0;
        }

        private void btnElegirArchivo_Click(object sender,
            RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog =
                new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                txtRutaArchivo.Text =
                    openFileDialog.FileName;
            }
        }

        private void btnReproducir_Click(object sender, RoutedEventArgs e)
        {
            
            if (output != null && 
                output.PlaybackState == PlaybackState.Paused)
            {
                output.Play();
                btnReproducir.IsEnabled = false;
                btnPausa.IsEnabled = true;
                btnDetener.IsEnabled = true;
            }
            else
            {
                reader =
                    new AudioFileReader(txtRutaArchivo.Text);

                delay =
                    new Delay(reader);
                delay.Activo = (bool)cbDelayActivo.IsChecked;
                delay.Inverso = (bool)cbAmplitudInvertidaActiva.IsChecked;

                delay.OffsetMilisegundos = (int)sldDelayOffset.Value;
                fades = new FadeInOutSampleProvider(
                    delay, true);
                double milisegundosFadeIn =
                    Double.Parse(txtDuracionFadeIn.Text)
                        * 1000.0;
                fades.BeginFadeIn(milisegundosFadeIn);
                fadingOut = false;
                output = new WaveOutEvent();

                output.DeviceNumber =
                    cbSalida.SelectedIndex;

                output.PlaybackStopped += Output_PlaybackStopped;

                volume =
                    new EfectoVolumen(fades);

                volume.Volume =
                    (float) sldVolumen.Value;

                output.Init(volume);
                output.Play();

                btnDetener.IsEnabled = true;
                btnPausa.IsEnabled = true;
                btnReproducir.IsEnabled = false;

                lblTiempoTotal.Text =
                    reader.TotalTime.ToString().Substring(0, 8);
                lblTiempoActual.Text =
                    reader.CurrentTime.ToString().Substring(0, 8);
                sldReproduccion.Maximum =
                    reader.TotalTime.TotalSeconds;
                sldReproduccion.Value =
                    reader.CurrentTime.TotalSeconds;

                timer.Start();
        
            }

        }

        private void Output_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            reader.Dispose();
            output.Dispose();
            timer.Stop();
        }

        private void btnPausa_Click(object sender, RoutedEventArgs e)
        {
            if (output != null)
            {
                output.Pause();
                btnDetener.IsEnabled = true;
                btnPausa.IsEnabled = false;
                btnReproducir.IsEnabled = true;
            }
        }

        private void btnDetener_Click(object sender, RoutedEventArgs e)
        {
            if (output != null)
            {
                output.Stop();
                btnReproducir.IsEnabled = true;
                btnPausa.IsEnabled = false;
                btnDetener.IsEnabled = false;
            }
        }

        private void sldReproduccion_DragStarted
            (object sender, 
            System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            dragging = true;
        }

        private void sldReproduccion_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            dragging = false;
            if (reader != null && output != null &&
                output.PlaybackState != PlaybackState.Stopped)
            {
                reader.CurrentTime =
                    TimeSpan.FromSeconds(sldReproduccion.Value);
            }
        }

        private void sldVolumen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (volume != null && output != null &&
                output.PlaybackState != PlaybackState.Stopped)
            {
                volume.Volume =
                    (float)sldVolumen.Value;
                
            }
            if (lblPorcentajeVolumen != null)
            {
                lblPorcentajeVolumen.Text =
                    ((int)(sldVolumen.Value * 100)).ToString()
                    + " %";
            }
            
        }

        private void btnFadeOut_Click(object sender, RoutedEventArgs e)
        {
            if(!fadingOut &&
                fades != null && output != null &&
                output.PlaybackState == PlaybackState.Playing)
            {
                fadingOut = true;
                double milisegundosFadeOut =
                    Double.Parse(txtDuracionFadeOut.Text) *
                    1000.0;
                fades.BeginFadeOut(milisegundosFadeOut);

            }
        }

        private void cbDelayActivo_Click(object sender, RoutedEventArgs e)
        {
            if (delay != null)
            {
                delay.Activo = (bool)cbDelayActivo.IsChecked;
            }
            
        }

        private void sldDelayOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (delay != null)
            {
                delay.OffsetMilisegundos = (int)sldDelayOffset.Value;
            }
        }

        private void cbAmplitudInvertidaActiva_Click(object sender, RoutedEventArgs e)
        {
            if (delay != null)
            {
                delay.Inverso = (bool)cbAmplitudInvertidaActiva.IsChecked;
            }
        }
    }
}
