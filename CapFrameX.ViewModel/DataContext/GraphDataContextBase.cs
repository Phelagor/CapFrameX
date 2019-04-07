﻿using CapFrameX.Contracts.Configuration;
using CapFrameX.OcatInterface;
using CapFrameX.Statistics;
using LiveCharts;
using Prism.Mvvm;

namespace CapFrameX.ViewModel.DataContext
{
	public class GraphDataContextBase : BindableBase
	{
		public const int SCALE_RESOLUTION = 200;

		private ZoomingOptions _zoomingMode;
		private SeriesCollection _seriesCollection;
		private bool _useRemovingOutlier;
		private bool _useSlidingWindow;
		private int _graphNumberSamples;
		private int _cutLeftSliderMaximum;
		private int _cutRightSliderMaximum;
		private bool _isCuttingModeActive;

		protected IAppConfiguration AppConfiguration { get; }

		protected IRecordDataServer RecordDataServer { get; }

		protected IStatisticProvider FrametimesStatisticProvider { get; }

		public bool IsCuttingModeActive
		{
			get { return _isCuttingModeActive; }
			set
			{
				_isCuttingModeActive = value;
				RaisePropertyChanged();
				OnCuttingModeChanged();
			}
		}

		public bool UseSlidingWindow
		{
			get { return _useSlidingWindow; }
			set
			{
				_useSlidingWindow = value;
				RaisePropertyChanged();
			}
		}

		public bool UseRemovingOutlier
		{
			get => _useRemovingOutlier;
			set
			{
				_useRemovingOutlier = value;
				RaisePropertyChanged();
			}
		}

		public Session RecordSession
		{
			get => RecordDataServer.CurrentSession;
			set
			{
				RecordDataServer.CurrentSession = value;
				InitializeCuttingParameter();
			}
		}

		public SeriesCollection SeriesCollection
		{
			get { return _seriesCollection; }
			set
			{
				_seriesCollection = value;
				RaisePropertyChanged();
			}
		}

		public ZoomingOptions ZoomingMode
		{
			get { return _zoomingMode; }
			set
			{
				_zoomingMode = value;
				RaisePropertyChanged();
			}
		}

		public int StartIndex
		{
			get { return RecordDataServer.StartIndex; }
			set
			{
				RecordDataServer.StartIndex = value;
				RaisePropertyChanged();
			}
		}

		public int EndIndex
		{
			get { return RecordDataServer.EndIndex; }
			set
			{
				RecordDataServer.EndIndex = value;
				RaisePropertyChanged();
			}
		}

		public double SelectedChartLength
		{
			get { return RecordDataServer.WindowLength; }
			set
			{
				RecordDataServer.WindowLength = value;
				RaisePropertyChanged();
			}
		}

		public double SliderValue
		{
			get { return RecordDataServer.CurrentTime; }
			set
			{
				RecordDataServer.CurrentTime = value;
				RaisePropertyChanged();
			}
		}

		public int CutLeftSliderMaximum
		{
			get { return _cutLeftSliderMaximum; }
			set
			{
				_cutLeftSliderMaximum = value;
				RaisePropertyChanged();
			}
		}

		public int CutRightSliderMaximum
		{
			get { return _cutRightSliderMaximum; }
			set
			{
				_cutRightSliderMaximum = value;
				RaisePropertyChanged();
			}
		}

		public int GraphNumberSamples
		{
			get { return _graphNumberSamples; }
			set
			{
				_graphNumberSamples = value;
				RaisePropertyChanged();
			}
		}

		public GraphDataContextBase(IRecordDataServer recordDataServer,
			IAppConfiguration appConfiguration, IStatisticProvider frametimesStatisticProvider)
		{
			RecordDataServer = recordDataServer;
			AppConfiguration = appConfiguration;
			FrametimesStatisticProvider = frametimesStatisticProvider;
			ZoomingMode = ZoomingOptions.Y;
		}

		private void OnCuttingModeChanged()
		{
			InitializeCuttingParameter();
		}

		public void InitializeCuttingParameter()
		{
			if (RecordSession == null)
				return;

			RecordDataServer.IsActive = false;

			StartIndex = 0;
			EndIndex = 0;

			CutLeftSliderMaximum = RecordSession.FrameTimes.Count / 2;
			CutRightSliderMaximum = RecordSession.FrameTimes.Count / 2;
			GraphNumberSamples = RecordSession.FrameTimes.Count;

			RecordDataServer.IsActive = true;
		}
	}
}