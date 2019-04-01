﻿using CapFrameX.Extensions;
using System;
using System.IO;

namespace CapFrameX.OcatInterface
{
	/// <summary>
	/// UI wrapper for record file info
	/// </summary>
	public class OcatRecordInfo
	{
		public string GameName { get; private set; }
		public string CreationDate { get; private set; }
		public string CreationTime { get; private set; }
		public string Comment { get; private set; } = "-";
		public string RecordTime { get; private set; }
		public string FullPath { get; private set; }
		public FileInfo FileInfo { get; private set; }
		public string CombinedInfo { get; private set; }

		private OcatRecordInfo(FileInfo fileInfo)
		{
			GameName = fileInfo.Name.Substring("OCAT-", ".exe");
			CreationDate = fileInfo.Name.Substring("exe-", "T");
			RecordTime = fileInfo.Name.Substring(CreationDate + "T", ".csv");
			CreationTime = fileInfo.LastWriteTime.ToString("HH:mm:ss");

			var commment = RecordManager.GetCommentFromRecordFile(fileInfo.FullName);

			if (commment != null)
			{
				Comment = commment;
			}

			FileInfo = fileInfo;
			FullPath = fileInfo.FullName;

			CombinedInfo = FullPath + Comment;
		}

		public static OcatRecordInfo Create(FileInfo fileInfo)
		{
			OcatRecordInfo recordInfo = null;

			try
			{
				recordInfo = new OcatRecordInfo(fileInfo);
			}
			catch (ArgumentException)
			{
				// Log
			}
			catch (Exception)
			{
				// Log
			}

			return recordInfo;
		}
	}
}
