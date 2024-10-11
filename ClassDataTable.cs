using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlChart
{
    internal class ClassDataTable
    {
        /// <summary>
        /// コントロール累積データ・テーブル
        /// </summary>
        public class D_CTRL_DATA_RESRV
        {
            public string KENSA_DATE { get; set; }
            public string TUBE_CODE { get; set; }
            public string K_CODE { get; set; }
            public int SUB_NO { get; set; }
            public int? DOSE_NO { get; set; }
            public string DOSE { get; set; }
            public string DOSE_OLD { get; set; }
            public DateTime? IMP_DATE { get; set; }
        }

        /// <summary>
        /// コントロールＱＣロット情報・テーブル
        /// </summary>
        public class D_CTRL_QCLOT_INFO
        {
            public string QCLOT_NO { get; set; }        // QCロットNo
            public string TUBE_CODE { get; set; }       // チューブコード
            public string K_CODE { get; set; }          // 項目コード
            public string S_DATE { get; set; }          // 開始日付
            public string SPEC_MEMO { get; set; }       // 特記事項
            public string LV_0 { get; set; }
            public string LV_1 { get; set; }
            public string LV_2 { get; set; }
            public string LV_3 { get; set; }
            public string LV_4 { get; set; }
            public string LV_5 { get; set; }
            public string LV_6 { get; set; }
            public string LV_7 { get; set; }
        }
    }
}
