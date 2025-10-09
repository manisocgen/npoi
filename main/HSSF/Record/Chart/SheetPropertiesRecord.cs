/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

using NPOI.HSSF.Record;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPOI.HSSF.Record.Chart
{
    /// <summary>
    /// Describes a chart sheet properties record. SHTPROPS (0x1044)
    /// 
    /// (As with all chart related records, documentation is lacking.
    /// See ChartRecord for more details)
    /// </summary>
    public sealed class SheetPropertiesRecord : StandardRecord
    {
        public const short _sid = 0x1044;

        public const byte EMPTY_NOT_PLOTTED = 0;
        public const byte EMPTY_ZERO = 1;
        public const byte EMPTY_INTERPOLATED = 2;

        private static readonly BitField ChartTypeManuallyFormatted = BitFieldFactory.GetInstance(0x01);
        private static readonly BitField PlotVisibleOnly = BitFieldFactory.GetInstance(0x02);
        private static readonly BitField DoNotSizeWithWindow = BitFieldFactory.GetInstance(0x04);
        private static readonly BitField DefaultPlotDimensions = BitFieldFactory.GetInstance(0x08);
        private static readonly BitField AutoPlotArea = BitFieldFactory.GetInstance(0x10);

        private int _field1Flags;
        private int _field2Empty;

        public SheetPropertiesRecord()
        {
        }

        public SheetPropertiesRecord(SheetPropertiesRecord other)
        {
            _field1Flags = other._field1Flags;
            _field2Empty = other._field2Empty;
        }

        public SheetPropertiesRecord(RecordInputStream input)
        {
            _field1Flags = input.ReadUShort();
            _field2Empty = input.ReadUShort();
        }

        public override void Serialize(ILittleEndianOutput output)
        {
            output.WriteShort(_field1Flags);
            output.WriteShort(_field2Empty);
        }

        protected override int DataSize => 2 + 2;

        public override short Sid => _sid;

        public override object Clone()
        {
            return new SheetPropertiesRecord(this);
        }

        /// <summary>
        /// Get the flags field for the SheetProperties record.
        /// </summary>
        public int Flags => _field1Flags;

        /// <summary>
        /// Get the empty field for the SheetProperties record.
        /// </summary>
        /// <returns>
        /// One of:
        /// EMPTY_NOT_PLOTTED
        /// EMPTY_ZERO
        /// EMPTY_INTERPOLATED
        /// </returns>
        public int Empty => _field2Empty;

        /// <summary>
        /// Set the empty field for the SheetProperties record.
        /// </summary>
        /// <param name="empty">
        /// One of:
        /// EMPTY_NOT_PLOTTED
        /// EMPTY_ZERO
        /// EMPTY_INTERPOLATED
        /// </param>
        public void SetEmpty(byte empty)
        {
            _field2Empty = empty;
        }

        /// <summary>
        /// Sets the chart type manually formatted field value.
        /// Has the chart type been manually formatted?
        /// </summary>
        public void SetChartTypeManuallyFormatted(bool value)
        {
            _field1Flags = ChartTypeManuallyFormatted.SetBoolean(_field1Flags, value);
        }

        /// <summary>
        /// Has the chart type been manually formatted?
        /// </summary>
        /// <returns>the chart type manually formatted field value.</returns>
        public bool IsChartTypeManuallyFormatted => ChartTypeManuallyFormatted.IsSet(_field1Flags);

        /// <summary>
        /// Sets the plot visible only field value.
        /// Only show visible cells on the chart.
        /// </summary>
        public void SetPlotVisibleOnly(bool value)
        {
            _field1Flags = PlotVisibleOnly.SetBoolean(_field1Flags, value);
        }

        /// <summary>
        /// Only show visible cells on the chart.
        /// </summary>
        /// <returns>the plot visible only field value.</returns>
        public bool IsPlotVisibleOnly => PlotVisibleOnly.IsSet(_field1Flags);

        /// <summary>
        /// Sets the do not size with window field value.
        /// Do not size the chart when the window changes size
        /// </summary>
        public void SetDoNotSizeWithWindow(bool value)
        {
            _field1Flags = DoNotSizeWithWindow.SetBoolean(_field1Flags, value);
        }

        /// <summary>
        /// Do not size the chart when the window changes size
        /// </summary>
        /// <returns>the do not size with window field value.</returns>
        public bool IsDoNotSizeWithWindow => DoNotSizeWithWindow.IsSet(_field1Flags);

        /// <summary>
        /// Sets the default plot dimensions field value.
        /// Indicates that the default area dimensions should be used.
        /// </summary>
        public void SetDefaultPlotDimensions(bool value)
        {
            _field1Flags = DefaultPlotDimensions.SetBoolean(_field1Flags, value);
        }

        /// <summary>
        /// Indicates that the default area dimensions should be used.
        /// </summary>
        /// <returns>the default plot dimensions field value.</returns>
        public bool IsDefaultPlotDimensions => DefaultPlotDimensions.IsSet(_field1Flags);

        /// <summary>
        /// Sets the auto plot area field value.
        /// ??
        /// </summary>
        public void SetAutoPlotArea(bool value)
        {
            _field1Flags = AutoPlotArea.SetBoolean(_field1Flags, value);
        }

        /// <summary>
        /// ??
        /// </summary>
        /// <returns>the auto plot area field value.</returns>
        public bool IsAutoPlotArea => AutoPlotArea.IsSet(_field1Flags);

        public override String ToString()
        {
            // Build flags description
            var flagNames = new List<string>();
            if (IsChartTypeManuallyFormatted) flagNames.Add("CHART_TYPE_MANUALLY_FORMATTED");
            if (IsPlotVisibleOnly) flagNames.Add("PLOT_VISIBLE_ONLY");
            if (IsDoNotSizeWithWindow) flagNames.Add("DO_NOT_SIZE_WITH_WINDOW");
            if (IsDefaultPlotDimensions) flagNames.Add("DEFAULT_PLOT_DIMENSIONS");
            if (IsAutoPlotArea) flagNames.Add("AUTO_PLOT_AREA");

            var flagsHex = $"0x{_field1Flags:X4}";
            var flagsDetail = flagNames.Count > 0 ? string.Join("|", flagNames) : "NONE";
            var emptyName = _field2Empty switch
            {
                EMPTY_NOT_PLOTTED => "EMPTY_NOT_PLOTTED",
                EMPTY_ZERO => "EMPTY_ZERO",
                EMPTY_INTERPOLATED => "EMPTY_INTERPOLATED",
                _ => $"UNKNOWN({_field2Empty})"
            };

            return $"SheetPropertiesRecord(flags={flagsHex} [{flagsDetail}], empty={emptyName}({_field2Empty}))";
        }
    }
}