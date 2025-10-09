
/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) Under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You Under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed Under the License is distributed on an "AS Is" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations Under the License.
==================================================================== */
/* ================================================================
 * About NPOI
 * POI Version: 3.8 beta4
 * Date: 2012-02-15
 * 
 * ==============================================================*/

namespace NPOI.HSSF.Record
{
    using NPOI.HSSF.Record.AutoFilter;
    using NPOI.HSSF.Record.Chart;
    using NPOI.HSSF.Record.PivotTable;
    using NPOI.Util;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /**
     * Title:  Record Factory
     * Description:  Takes a stream and outputs an array of Record objects.
     *
     * @deprecated use {@link org.apache.poi.hssf.eventmodel.EventRecordFactory} instead
     * @see org.apache.poi.hssf.eventmodel.EventRecordFactory
     * @author Andrew C. Oliver (acoliver at apache dot org)
     * @author Marc Johnson (mjohnson at apache dot org)
     * @author Glen Stampoultzis (glens at apache.org)
     * @author Csaba Nagy (ncsaba at yahoo dot com)
     */

    public class RecordFactory
    {
        private static int NUM_RECORDS = 512;
        #region inner Record Creater
        private interface I_RecordCreator
        {
            Record Create(RecordInputStream in1);

            Type GetRecordClass();
        }

        private sealed class ReflectionConstructorRecordCreator : I_RecordCreator
        {

            private readonly ConstructorInfo _c;
            public ReflectionConstructorRecordCreator(ConstructorInfo c)
            {
                _c = c;
            }
            public Record Create(RecordInputStream in1)
            {
                Object[] args = { in1 };
                try
                {
                    return (Record)_c.Invoke(args);
                }
                catch (Exception e)
                {
                    throw new RecordFormatException("Unable to construct record instance", e.InnerException);
                }
            }
            public Type GetRecordClass()
            {
                return _c.DeclaringType;
            }
        }
        /**
         * A "create" method is used instead of the usual constructor if the created record might
         * be of a different class to the declaring class.
         */
        private sealed class ReflectionMethodRecordCreator : I_RecordCreator
        {

            private readonly MethodInfo _m;
            public ReflectionMethodRecordCreator(MethodInfo m)
            {
                _m = m;
            }
            public Record Create(RecordInputStream in1)
            {
                Object[] args = { in1 };
                try
                {
                    return (Record)_m.Invoke(null, args);
                }
                catch (Exception e)
                {
                    throw new RecordFormatException("Unable to construct record instance", e.InnerException);
                }
            }
            public Type GetRecordClass()
            {
                return _m.DeclaringType;
            }
        }
        #endregion

        private static readonly Type[] CONSTRUCTOR_ARGS = new Type[] { typeof(RecordInputStream), };


        static RecordFactory()
        {
            var hssfRecordTypes = new Dictionary<HSSFRecordTypes, Type>()
            {
                { HSSFRecordTypes.UNKNOWN, typeof(UnknownRecord) },
                { HSSFRecordTypes.FORMULA, typeof(FormulaRecord) },
                { HSSFRecordTypes.EOF, typeof(EOFRecord) },
                { HSSFRecordTypes.CALC_COUNT, typeof(CalcCountRecord) },
                { HSSFRecordTypes.CALC_MODE, typeof(CalcModeRecord) },
                { HSSFRecordTypes.PRECISION, typeof(PrecisionRecord) },
                { HSSFRecordTypes.REF_MODE, typeof(RefModeRecord) },
                { HSSFRecordTypes.DELTA, typeof(DeltaRecord) },
                { HSSFRecordTypes.ITERATION, typeof(IterationRecord) },
                { HSSFRecordTypes.PROTECT, typeof(ProtectRecord) },
                { HSSFRecordTypes.PASSWORD, typeof(PasswordRecord) },
                { HSSFRecordTypes.HEADER, typeof(HeaderRecord) },
                { HSSFRecordTypes.FOOTER, typeof(FooterRecord) },
                { HSSFRecordTypes.EXTERN_SHEET, typeof(ExternSheetRecord) },
                { HSSFRecordTypes.NAME, typeof(NameRecord) },
                { HSSFRecordTypes.WINDOW_PROTECT, typeof(WindowProtectRecord) },
                { HSSFRecordTypes.VERTICAL_PAGE_BREAK, typeof(VerticalPageBreakRecord) },
                { HSSFRecordTypes.HORIZONTAL_PAGE_BREAK, typeof(HorizontalPageBreakRecord) },
                { HSSFRecordTypes.NOTE, typeof(NoteRecord) },
                { HSSFRecordTypes.SELECTION, typeof(SelectionRecord) },
                { HSSFRecordTypes.DATE_WINDOW_1904, typeof(DateWindow1904Record) },
                { HSSFRecordTypes.EXTERNAL_NAME, typeof(ExternalNameRecord) },
                { HSSFRecordTypes.LEFT_MARGIN, typeof(LeftMarginRecord) },
                { HSSFRecordTypes.RIGHT_MARGIN, typeof(RightMarginRecord) },
                { HSSFRecordTypes.TOP_MARGIN, typeof(TopMarginRecord) },
                { HSSFRecordTypes.BOTTOM_MARGIN, typeof(BottomMarginRecord) },
                { HSSFRecordTypes.PRINT_HEADERS, typeof(PrintHeadersRecord) },
                { HSSFRecordTypes.PRINT_GRIDLINES, typeof(PrintGridlinesRecord) },
                { HSSFRecordTypes.FILE_PASS, typeof(FilePassRecord) },
                { HSSFRecordTypes.FONT, typeof(FontRecord) },
                { HSSFRecordTypes.CONTINUE, typeof(ContinueRecord) },
                { HSSFRecordTypes.WINDOW_ONE, typeof(WindowOneRecord) },
                { HSSFRecordTypes.BACKUP, typeof(BackupRecord) },
                { HSSFRecordTypes.PANE, typeof(PaneRecord) },
                { HSSFRecordTypes.CODEPAGE, typeof(CodepageRecord) },
                { HSSFRecordTypes.DCON_REF, typeof(DConRefRecord) },
                { HSSFRecordTypes.DEFAULT_COL_WIDTH, typeof(DefaultColWidthRecord) },
                { HSSFRecordTypes.CRN_COUNT, typeof(CRNCountRecord) },
                { HSSFRecordTypes.CRN, typeof(CRNRecord) },
                { HSSFRecordTypes.WRITE_ACCESS, typeof(WriteAccessRecord) },
                { HSSFRecordTypes.FILE_SHARING, typeof(FileSharingRecord) },
                { HSSFRecordTypes.OBJ, typeof(ObjRecord) },
                { HSSFRecordTypes.UNCALCED, typeof(UncalcedRecord) },
                { HSSFRecordTypes.SAVE_RECALC, typeof(SaveRecalcRecord) },
                { HSSFRecordTypes.OBJECT_PROTECT, typeof(ObjectProtectRecord) },
                { HSSFRecordTypes.COLUMN_INFO, typeof(ColumnInfoRecord) },
                { HSSFRecordTypes.GUTS, typeof(GutsRecord) },
                { HSSFRecordTypes.WS_BOOL, typeof(WSBoolRecord) },
                { HSSFRecordTypes.GRIDSET, typeof(GridsetRecord) },
                { HSSFRecordTypes.H_CENTER, typeof(HCenterRecord) },
                { HSSFRecordTypes.V_CENTER, typeof(VCenterRecord) },
                { HSSFRecordTypes.BOUND_SHEET, typeof(BoundSheetRecord) },
                { HSSFRecordTypes.WRITE_PROTECT, typeof(WriteProtectRecord) },
                { HSSFRecordTypes.COUNTRY, typeof(CountryRecord) },
                { HSSFRecordTypes.HIDE_OBJ, typeof(HideObjRecord) },
                { HSSFRecordTypes.PALETTE, typeof(PaletteRecord) },
                { HSSFRecordTypes.FN_GROUP_COUNT, typeof(FnGroupCountRecord) },
                { HSSFRecordTypes.AUTO_FILTER_INFO, typeof(AutoFilterInfoRecord) },
                { HSSFRecordTypes.SCL, typeof(SCLRecord) },
                { HSSFRecordTypes.PRINT_SETUP, typeof(PrintSetupRecord) },
                { HSSFRecordTypes.VIEW_DEFINITION, typeof(ViewDefinitionRecord) },
                { HSSFRecordTypes.VIEW_FIELDS, typeof(ViewFieldsRecord) },
                { HSSFRecordTypes.PAGE_ITEM, typeof(PageItemRecord) },
                { HSSFRecordTypes.MUL_BLANK, typeof(MulBlankRecord) },
                { HSSFRecordTypes.MUL_RK, typeof(MulRKRecord) },
                { HSSFRecordTypes.MMS, typeof(MMSRecord) },
                { HSSFRecordTypes.DATA_ITEM, typeof(DataItemRecord) },
                { HSSFRecordTypes.STREAM_ID, typeof(StreamIDRecord) },
                { HSSFRecordTypes.DB_CELL, typeof(DBCellRecord) },
                { HSSFRecordTypes.BOOK_BOOL, typeof(BookBoolRecord) },
                { HSSFRecordTypes.SCENARIO_PROTECT, typeof(ScenarioProtectRecord) },
                { HSSFRecordTypes.EXTENDED_FORMAT, typeof(ExtendedFormatRecord) },
                { HSSFRecordTypes.INTERFACE_HDR, typeof(InterfaceHdrRecord) },
                { HSSFRecordTypes.INTERFACE_END, typeof(InterfaceEndRecord) },
                { HSSFRecordTypes.VIEW_SOURCE, typeof(ViewSourceRecord) },
                { HSSFRecordTypes.MERGE_CELLS, typeof(MergeCellsRecord) },
                { HSSFRecordTypes.DRAWING_GROUP, typeof(DrawingGroupRecord) },
                { HSSFRecordTypes.DRAWING, typeof(DrawingRecord) },
                { HSSFRecordTypes.DRAWING_SELECTION, typeof(DrawingSelectionRecord) },
                { HSSFRecordTypes.SST, typeof(SSTRecord) },
                { HSSFRecordTypes.LABEL_SST, typeof(LabelSSTRecord) },
                { HSSFRecordTypes.EXT_SST, typeof(ExtSSTRecord) },
                { HSSFRecordTypes.EXTENDED_PIVOT_TABLE_VIEW_FIELDS, typeof(ExtendedPivotTableViewFieldsRecord) },
                { HSSFRecordTypes.TAB_ID, typeof(TabIdRecord) },
                { HSSFRecordTypes.USE_SEL_FS, typeof(UseSelFSRecord) },
                { HSSFRecordTypes.DSF, typeof(DSFRecord) },
                { HSSFRecordTypes.USER_SVIEW_BEGIN, typeof(UserSViewBegin) },
                { HSSFRecordTypes.USER_SVIEW_END, typeof(UserSViewEnd) },
                { HSSFRecordTypes.SUP_BOOK, typeof(SupBookRecord) },
                { HSSFRecordTypes.PROTECTION_REV_4, typeof(ProtectionRev4Record) },
                { HSSFRecordTypes.CF_HEADER, typeof(CFHeaderRecord) },
                { HSSFRecordTypes.CF_RULE, typeof(CFRuleRecord) },
                { HSSFRecordTypes.DVAL, typeof(DVALRecord) },
                { HSSFRecordTypes.TEXT_OBJECT, typeof(TextObjectRecord) },
                { HSSFRecordTypes.REFRESH_ALL, typeof(RefreshAllRecord) },
                { HSSFRecordTypes.HYPERLINK, typeof(HyperlinkRecord) },
                { HSSFRecordTypes.PASSWORD_REV_4, typeof(PasswordRev4Record) },
                { HSSFRecordTypes.DV, typeof(DVRecord) },
                { HSSFRecordTypes.RECALC_ID, typeof(RecalcIdRecord) },
                { HSSFRecordTypes.DIMENSIONS, typeof(DimensionsRecord) },
                { HSSFRecordTypes.BLANK, typeof(BlankRecord) },
                { HSSFRecordTypes.NUMBER, typeof(NumberRecord) },
                { HSSFRecordTypes.LABEL, typeof(LabelRecord) },
                { HSSFRecordTypes.BOOL_ERR, typeof(BoolErrRecord) },
                { HSSFRecordTypes.STRING, typeof(StringRecord) },
                { HSSFRecordTypes.ROW, typeof(RowRecord) },
                { HSSFRecordTypes.INDEX, typeof(IndexRecord) },
                { HSSFRecordTypes.ARRAY, typeof(ArrayRecord) },
                { HSSFRecordTypes.DEFAULT_ROW_HEIGHT, typeof(DefaultRowHeightRecord) },
                { HSSFRecordTypes.TABLE, typeof(TableRecord) },
                { HSSFRecordTypes.WINDOW_TWO, typeof(WindowTwoRecord) },
                { HSSFRecordTypes.RK, typeof(RKRecord) },
                { HSSFRecordTypes.STYLE, typeof(StyleRecord) },
                { HSSFRecordTypes.FORMAT, typeof(FormatRecord) },
                { HSSFRecordTypes.SHARED_FORMULA, typeof(SharedFormulaRecord) },
                { HSSFRecordTypes.BOF, typeof(BOFRecord) },
                { HSSFRecordTypes.CHART_FRT_INFO, typeof(ChartFRTInfoRecord) },
                { HSSFRecordTypes.CHART_START_BLOCK, typeof(ChartStartBlockRecord) },
                { HSSFRecordTypes.CHART_END_BLOCK, typeof(ChartEndBlockRecord) },
                { HSSFRecordTypes.CHART_START_OBJECT, typeof(ChartStartObjectRecord) },
                { HSSFRecordTypes.CHART_END_OBJECT, typeof(ChartEndObjectRecord) },
                { HSSFRecordTypes.CAT_LAB, typeof(CatLabRecord) },
                { HSSFRecordTypes.FEAT_HDR, typeof(FeatHdrRecord) },
                { HSSFRecordTypes.FEAT, typeof(FeatRecord) },
                { HSSFRecordTypes.DATA_LABEL_EXTENSION, typeof(DataLabelExtensionRecord) },
                { HSSFRecordTypes.CF_HEADER_12, typeof(CFHeader12Record) },
                { HSSFRecordTypes.CF_RULE_12, typeof(CFRule12Record) },
                { HSSFRecordTypes.TABLE_STYLES, typeof(TableStylesRecord) },
                { HSSFRecordTypes.NAME_COMMENT, typeof(NameCommentRecord) },
                { HSSFRecordTypes.HEADER_FOOTER, typeof(HeaderFooterRecord) },
                { HSSFRecordTypes.UNITS, typeof(UnitsRecord) },
                { HSSFRecordTypes.CHART, typeof(ChartRecord) },
                { HSSFRecordTypes.SERIES, typeof(SeriesRecord) },
                { HSSFRecordTypes.DATA_FORMAT, typeof(DataFormatRecord) },
                { HSSFRecordTypes.LINE_FORMAT, typeof(LineFormatRecord) },
                { HSSFRecordTypes.AREA_FORMAT, typeof(AreaFormatRecord) },
                { HSSFRecordTypes.SERIES_LABELS, typeof(SeriesLabelsRecord) },
                { HSSFRecordTypes.SERIES_TEXT, typeof(SeriesTextRecord) },
                { HSSFRecordTypes.CHART_FORMAT, typeof(ChartFormatRecord) },
                { HSSFRecordTypes.LEGEND, typeof(LegendRecord) },
                { HSSFRecordTypes.SERIES_LIST, typeof(SeriesListRecord) },
                { HSSFRecordTypes.BAR, typeof(BarRecord) },
                { HSSFRecordTypes.AREA, typeof(AreaRecord) },
                { HSSFRecordTypes.AXIS, typeof(AxisRecord) },
                { HSSFRecordTypes.TICK, typeof(TickRecord) },
                { HSSFRecordTypes.VALUE_RANGE, typeof(ValueRangeRecord) },
                { HSSFRecordTypes.CATEGORY_SERIES_AXIS, typeof(CategorySeriesAxisRecord) },
                { HSSFRecordTypes.AXIS_LINE_FORMAT, typeof(AxisLineFormatRecord) },
                { HSSFRecordTypes.DEFAULT_DATA_LABEL_TEXT_PROPERTIES, typeof(DefaultDataLabelTextPropertiesRecord) },
                { HSSFRecordTypes.TEXT, typeof(TextRecord) },
                { HSSFRecordTypes.FONT_INDEX, typeof(FontIndexRecord) },
                { HSSFRecordTypes.OBJECT_LINK, typeof(ObjectLinkRecord) },
                { HSSFRecordTypes.FRAME, typeof(FrameRecord) },
                { HSSFRecordTypes.BEGIN, typeof(BeginRecord) },
                { HSSFRecordTypes.END, typeof(EndRecord) },
                { HSSFRecordTypes.PLOT_AREA, typeof(PlotAreaRecord) },
                { HSSFRecordTypes.AXIS_PARENT, typeof(AxisParentRecord) },
                { HSSFRecordTypes.SHEET_PROPERTIES, typeof(SheetPropertiesRecord) },
                { HSSFRecordTypes.SERIES_CHART_GROUP_INDEX, typeof(SeriesChartGroupIndexRecord) },
                { HSSFRecordTypes.AXIS_USED, typeof(AxisUsedRecord) },
                { HSSFRecordTypes.NUMBER_FORMAT_INDEX, typeof(NumberFormatIndexRecord) },
                { HSSFRecordTypes.CHART_TITLE_FORMAT, typeof(ChartTitleFormatRecord) },
                { HSSFRecordTypes.LINKED_DATA, typeof(LinkedDataRecord) },
                { HSSFRecordTypes.FONT_BASIS, typeof(FontBasisRecord) },
                { HSSFRecordTypes.AXIS_OPTIONS, typeof(AxisOptionsRecord) },
                { HSSFRecordTypes.DAT, typeof(DatRecord) },
                { HSSFRecordTypes.PLOT_GROWTH, typeof(PlotGrowthRecord) },
                { HSSFRecordTypes.SERIES_INDEX, typeof(SeriesIndexRecord) },
                { HSSFRecordTypes.ESCHER_AGGREGATE, typeof(EscherAggregate) }
            };

            _mapRecordTypeBySid = hssfRecordTypes.ToDictionary(kvp => kvp.Value, kvp => (short)kvp.Key);
            _recordCreatorsById = RecordsToMap(hssfRecordTypes.Values.ToArray());
        }

        //private static Hashtable recordsMap;
        /**
	     * cache of the recordsToMap();
	     */
        private static readonly Dictionary<short, I_RecordCreator> _recordCreatorsById = null;//RecordsToMap(recordClasses);

        // Fast sid -> Type map
        private static readonly Dictionary<Type, short> _mapRecordTypeBySid = null;

        private static short[] _allKnownRecordSIDs;
        /**
         * Debug / diagnosis method<br/>
         * Gets the POI implementation class for a given <tt>sid</tt>.  Only a subset of the any BIFF
         * records are actually interpreted by POI.  A few others are known but not interpreted
         * (see {@link UnknownRecord#getBiffName(int)}).
         * @return the POI implementation class for the specified record <tt>sid</tt>.
         * <code>null</code> if the specified record is not interpreted by POI.
         */
        public static Type GetRecordClass(int sid)
        {
            I_RecordCreator rc = null;
            if (_recordCreatorsById.ContainsKey((short)sid))
                rc = _recordCreatorsById[(short)sid];

            if (rc == null)
            {
                return null;
            }
            return rc.GetRecordClass();
        }
        /**
         * Changes the default capacity (10000) to handle larger files
         */

        public static void SetCapacity(int capacity)
        {
            NUM_RECORDS = capacity;
        }

        /**
         * Create an array of records from an input stream
         *
         * @param in the InputStream from which the records will be
         *           obtained
         *
         * @return an array of Records Created from the InputStream
         *
         * @exception RecordFormatException on error Processing the
         *            InputStream
         */

        public static List<Record> CreateRecords(InputStream in1)
        {
            List<Record> records = new List<Record>(NUM_RECORDS);


            RecordFactoryInputStream recStream = new RecordFactoryInputStream(in1, true);

            Record record;
            while ((record = recStream.NextRecord()) != null)
            {
                records.Add(record);
            }

            return records;
        }
        [Obsolete]
        private static void AddAll(List<Record> destList, Record[] srcRecs)
        {
            for (int i = 0; i < srcRecs.Length; i++)
            {
                destList.Add(srcRecs[i]);
            }
        }


        public static Record[] CreateRecord(RecordInputStream in1)
        {
            Record record = CreateSingleRecord(in1);
            if (record is DBCellRecord)
            {
                // Not needed by POI.  Regenerated from scratch by POI when spreadsheet is written
                return new Record[] { null, };
            }
            if (record is RKRecord rkRecord)
            {
                return new Record[] { ConvertToNumberRecord(rkRecord), };
            }
            if (record is MulRKRecord mulRkRecord)
            {
                return ConvertRKRecords(mulRkRecord);
            }
            return new Record[] { record, };
        }
        /**
         * Converts a {@link MulBlankRecord} into an equivalent array of {@link BlankRecord}s
         */
        public static BlankRecord[] ConvertBlankRecords(MulBlankRecord mbk)
        {
            BlankRecord[] mulRecs = new BlankRecord[mbk.NumColumns];
            for (int k = 0; k < mbk.NumColumns; k++)
            {
                BlankRecord br = new BlankRecord();

                br.Column = k + mbk.FirstColumn;
                br.Row = mbk.Row;
                br.XFIndex = mbk.GetXFAt(k);
                mulRecs[k] = br;
            }
            return mulRecs;
        }

        public static Record CreateSingleRecord(RecordInputStream in1)
        {
            if (_recordCreatorsById.TryGetValue(in1.Sid, out I_RecordCreator constructor))
            {
                return constructor.Create(in1);
            }
            else
            {
                return new UnknownRecord(in1);
            }
        }
        /// <summary>
        /// RK record is a slightly smaller alternative to NumberRecord
        /// POI likes NumberRecord better
        /// </summary>
        /// <param name="rk">The rk.</param>
        /// <returns></returns>
        public static NumberRecord ConvertToNumberRecord(RKRecord rk)
        {
            NumberRecord num = new NumberRecord();

            num.Column = (rk.Column);
            num.Row = (rk.Row);
            num.XFIndex = (rk.XFIndex);
            num.Value = (rk.RKNumber);
            return num;
        }
        /// <summary>
        /// Converts a MulRKRecord into an equivalent array of NumberRecords
        /// </summary>
        /// <param name="mrk">The MRK.</param>
        /// <returns></returns>
        public static NumberRecord[] ConvertRKRecords(MulRKRecord mrk)
        {

            NumberRecord[] mulRecs = new NumberRecord[mrk.NumColumns];
            for (int k = 0; k < mrk.NumColumns; k++)
            {
                NumberRecord nr = new NumberRecord();

                nr.Column = ((short)(k + mrk.FirstColumn));
                nr.Row = (mrk.Row);
                nr.XFIndex = (mrk.GetXFAt(k));
                nr.Value = (mrk.GetRKNumberAt(k));
                mulRecs[k] = nr;
            }
            return mulRecs;
        }
        public static short[] GetAllKnownRecordSIDs()
        {
            if (_allKnownRecordSIDs == null)
            {
                short[] results = new short[_recordCreatorsById.Count];
                int i = 0;

                foreach (KeyValuePair<short, I_RecordCreator> kv in _recordCreatorsById)
                {
                    results[i++] = kv.Key;
                }
                Array.Sort(results);
                _allKnownRecordSIDs = results;
            }

            return (short[])_allKnownRecordSIDs.Clone();
        }

        private static Dictionary<short, I_RecordCreator> RecordsToMap(Type[] records)
        {
            Dictionary<short, I_RecordCreator> result = new Dictionary<short, I_RecordCreator>();
            Hashtable uniqueRecClasses = new Hashtable(records.Length * 3 / 2);

            for (int i = 0; i < records.Length; i++)
            {

                Type recClass = records[i];
                if (!typeof(Record).IsAssignableFrom(recClass))
                {
                    throw new Exception("Invalid record sub-class (" + recClass.Name + ")");
                }
                if (recClass.IsAbstract)
                {
                    throw new Exception("Invalid record class (" + recClass.Name + ") - must not be abstract");
                }
                if (uniqueRecClasses.Contains(recClass))
                {
                    throw new Exception("duplicate record class (" + recClass.Name + ")");
                }
                uniqueRecClasses.Add(recClass, recClass);

                if (!_mapRecordTypeBySid.TryGetValue(recClass, out short sid) || sid == 0)
                {
                    throw new RecordFormatException("Unable to determine record types");
                }

                if (result.TryGetValue(sid, out I_RecordCreator value))
                {
                    Type prevClass = value.GetRecordClass();
                    throw new RuntimeException("duplicate record sid 0x" + sid.ToString("X", CultureInfo.CurrentCulture)
                            + " for classes (" + recClass.Name + ") and (" + prevClass.Name + ")");
                }
                result[sid] = GetRecordCreator(recClass);
            }
            return result;
        }
        [Obsolete]
        private static void CheckZeros(Stream in1, int avail)
        {
            int count = 0;
            while (true)
            {
                int b = (int)in1.ReadByte();
                if (b < 0)
                {
                    break;
                }
                if (b != 0)
                {
                    Console.Error.WriteLine(HexDump.ByteToHex(b));
                }
                count++;
            }
            if (avail != count)
            {
                Console.Error.WriteLine("avail!=count (" + avail + "!=" + count + ").");
            }
        }

        private static I_RecordCreator GetRecordCreator(Type recClass)
        {
            try
            {
                ConstructorInfo constructor;
                constructor = recClass.GetConstructor(CONSTRUCTOR_ARGS);
                if (constructor != null)
                    return new ReflectionConstructorRecordCreator(constructor);
            }
            catch
            {
                // fall through and look for other construction methods
            }
            try
            {
                MethodInfo m = recClass.GetMethod("Create", CONSTRUCTOR_ARGS);
                return new ReflectionMethodRecordCreator(m);
            }
            catch
            {
                throw new RuntimeException("Failed to find constructor or create method for (" + recClass.Name + ").");
            }
        }

    }
}
