using Planar.Service.Model.DataObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    public partial class JobInstanceLog : IJobInstanceLogForStatistics
    {
        [NotMapped]
        public bool? IsOutlier => Anomaly == null ? null : Anomaly > 0;

        [NotMapped]
        public string AnomalyTitle
        {
            get
            {
                AnomalyMembers result = AnomalyMembers.Undefined;
                if (Anomaly != null)
                {
                    try
                    {
                        result = (AnomalyMembers)Anomaly.GetValueOrDefault();
                    }
                    catch
                    {
                        // *** DO NOTHING *** //
                    }
                }

                return result.ToString();
            }
        }
    }
}