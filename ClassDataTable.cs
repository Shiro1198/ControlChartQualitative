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
            public string MNGVAL { get; set; }          // 管理値
            public string DAYOVER_CV { get; set; }      // 日間CV
            public string DAYIN_CV { get; set; }        // 日内CV
            public string S_DATE { get; set; }          // 開始日付
            public string SPEC_MEMO { get; set; }       // 特記事項
        }
    }
}
