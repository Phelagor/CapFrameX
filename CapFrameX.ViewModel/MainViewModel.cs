﻿using CapFrameX.Contracts.OcatInterface;
using CapFrameX.OcatInterface;
using Prism.Mvvm;
using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Threading;
using System.Reactive.Linq;
using System.Linq;
using LiveCharts;
using System.Windows.Input;
using Prism.Commands;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using CapFrameX.Statistics;

namespace CapFrameX.ViewModel
{
	public class MainViewModel : BindableBase
	{
		private readonly IRecordDirectoryObserver _recordObserver;
		private readonly IStatisticProvider _frametimeStatisticProvider;

		private OcatRecordInfo _selectedRecordInfo;
		private ZoomingOptions _zoomingMode;
		private SeriesCollection _seriesCollection;
		private SeriesCollection _statisticCollection;
		private string[] _parameterLabels;
		private int _firstNFrames;
		private int _lastNFrames;
		private Session _session;
		private bool _removeOutliers;
		private bool _useAdaptiveVariance;

		public Func<double, string> ParameterFormatter { get; set; } = value => value.ToString("N");

		public string[] ParameterLabels
		{
			get { return _parameterLabels; }
			set
			{
				_parameterLabels = value;
				RaisePropertyChanged();
			}
		}

		public SeriesCollection StatisticCollection
		{
			get { return _statisticCollection; }
			set
			{
				_statisticCollection = value;
				RaisePropertyChanged();
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

		public OcatRecordInfo SelectedRecordInfo
		{
			get { return _selectedRecordInfo; }
			set
			{
				_selectedRecordInfo = value;
				RaisePropertyChanged();
				OnSelectedRecordInfoChanged();
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

		public int FirstNFrames
		{
			get { return _firstNFrames; }
			set
			{
				_firstNFrames = value;
				RaisePropertyChanged();
				UpdateCharts();
			}
		}

		public int LastNFrames
		{
			get { return _lastNFrames; }
			set
			{
				_lastNFrames = value;
				RaisePropertyChanged();
				UpdateCharts();
			}
		}

		public bool RemoveOutliers
		{
			get { return _removeOutliers; }
			set
			{
				_removeOutliers = value;
				RaisePropertyChanged();
				UpdateCharts();
			}
		}

		public bool UseAdaptiveVariance
		{
			get { return _useAdaptiveVariance; }
			set
			{
				_useAdaptiveVariance = value;
				RaisePropertyChanged();
				UpdateCharts();
			}
		}

		public ObservableCollection<OcatRecordInfo> RecordInfoList { get; }
			= new ObservableCollection<OcatRecordInfo>();

		public ICommand ToogleZoomingModeCommand { get; }

		public MainViewModel(IRecordDirectoryObserver recordObserver, IStatisticProvider frametimeStatisticProvider)
		{
			_recordObserver = recordObserver;
			_frametimeStatisticProvider = frametimeStatisticProvider;

			// ToDo: check wether to do this async
			var initialRecordList = _recordObserver.GetAllRecordFileInfo();

			foreach (var fileInfo in initialRecordList)
			{
				AddToRecordInfoList(fileInfo);
			}

			var context = SynchronizationContext.Current;
			_recordObserver.RecordCreatedStream.ObserveOn(context).SubscribeOn(context)
							.Subscribe(OnRecordCreated);
			_recordObserver.RecordDeletedStream.ObserveOn(context).SubscribeOn(context)
							.Subscribe(OnRecordDeleted);

			// Turn streams now on
			_recordObserver.IsActive = true;

			ToogleZoomingModeCommand = new DelegateCommand(OnToogleZoomingMode);

			ZoomingMode = ZoomingOptions.Y;
		}

		private void OnToogleZoomingMode()
		{
			switch (ZoomingMode)
			{
				case ZoomingOptions.None:
					ZoomingMode = ZoomingOptions.X;
					break;
				case ZoomingOptions.X:
					ZoomingMode = ZoomingOptions.Y;
					break;
				case ZoomingOptions.Y:
					ZoomingMode = ZoomingOptions.Xy;
					break;
				case ZoomingOptions.Xy:
					ZoomingMode = ZoomingOptions.None;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void AddToRecordInfoList(FileInfo fileInfo)
		{
			var recordInfo = OcatRecordInfo.Create(fileInfo);
			if (recordInfo != null)
			{
				RecordInfoList.Add(recordInfo);
			}
		}

		private void OnRecordCreated(FileInfo fileInfo) => AddToRecordInfoList(fileInfo);

		private void OnRecordDeleted(FileInfo fileInfo)
		{
			var recordInfo = OcatRecordInfo.Create(fileInfo);
			if (recordInfo != null)
			{
				var match = RecordInfoList.FirstOrDefault(info => info.FullPath == fileInfo.FullName);

				if (match != null)
				{
					RecordInfoList.Remove(match);
				}
			}
		}

		private void OnSelectedRecordInfoChanged()
		{
			_session = RecordManager.LoadData(SelectedRecordInfo.FullPath);

			if (_session.FrameTimes != null && _session.FrameTimes.Any())
			{
				if (FirstNFrames != 0 || LastNFrames != 0)
				{
					var subset = GetFrametimesSubset();
					SetFrametimeChart(subset);
					SetStaticChart(subset);
				}
				else
				{
					SetFrametimeChart(GetFrametimes());
					SetStaticChart(GetFrametimes());
				}
			}
		}

		private IList<double> GetFrametimes()
		{
			if (RemoveOutliers)
			{
				// ToDo: Make method selectable
				return _frametimeStatisticProvider.GetOutlierAdjustedSequence(_session.FrameTimes, ERemoveOutlierMethod.DeciPercentile);
			}
			else
			{
				return _session.FrameTimes;
			}
		}

		private void SetFrametimeChart(IList<double> frametimes)
		{
			var gradientBrush = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(0, 1)
			};

			gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(139, 35, 35), 0));
			gradientBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1));

			var values = new GearedValues<double>();
			values.AddRange(frametimes);
			values.WithQuality(Quality.High);

			SeriesCollection = new SeriesCollection
			{
				new GLineSeries
				{
					Values = values,
					Fill = gradientBrush,
					Stroke = new SolidColorBrush(Color.FromRgb(139,35,35)),
					StrokeThickness = 1,
					LineSmoothness= 0,
					PointGeometrySize = 0
				}
			};
		}

		private void SetStaticChart(IList<double> frameTimes)
		{
			var fps = frameTimes.Select(ft => 1000 / ft).ToList();
			var average = Math.Round(fps.Average(), 0);
			var p1_quantile = Math.Round(_frametimeStatisticProvider.GetPQuantileSequence(fps, 0.01), 0);
			var p5_quantile = Math.Round(_frametimeStatisticProvider.GetPQuantileSequence(fps, 0.05), 0);
			var min = Math.Round(fps.Min(), 0);

			StatisticCollection = new SeriesCollection
			{
				new RowSeries
				{
					Title = SelectedRecordInfo.GameName,
					Fill = new SolidColorBrush(Color.FromRgb(83,104,114)),
					Values = new ChartValues<double> { min, p1_quantile, p5_quantile, average },
					DataLabels = true
				}
			};

			ParameterLabels = new[] { "Min", "1%", "5%", "Average" };
		}

		private void UpdateCharts()
		{
			var subset = GetFrametimesSubset();
			SetFrametimeChart(subset);
			SetStaticChart(subset);
		}

		private List<double> GetFrametimesSubset()
		{
			var subset = new List<double>();
			var frametimes = GetFrametimes();

			if (frametimes != null && frametimes.Any())
			{
				for (int i = FirstNFrames; i < frametimes.Count - LastNFrames; i++)
				{
					subset.Add(frametimes[i]);
				}
			}

			return subset;
		}
	}
}