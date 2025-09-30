using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    public class ApprovalExtension
    {
        public ApprovalExtension(/* User user, IntPtr parentWindowHandle */)
        {
            // TODO: Add here the code that constructs the extension. Extension state is kept alive during PrePromote and PostPromote phases.
        }

        public PlanValidationResult PrePromote(PlanSetupApprovalStatus newStatus, IEnumerable<PlanSetup> plans)
        {
            PlanValidationResult result = new PlanValidationResult();
            // TODO: Add here the code that is executed before the Eclipse Planning Approval wizard is launched.
            //Extract the beam names and the calculated names.
            PlanSetup plan = plans.FirstOrDefault();//TODO: Start working for plan sum.
            Dictionary<string,string> beamNames = new Dictionary<string,string>();
            Dictionary<string, string> calculatedBeamNames = new Dictionary<string, string>();
            foreach(var beam in plan.Beams)
            {
                beamNames.Add(beam.Id, beam.Name);
                string calculatedName = GetProperBeamName(plan, beam);
                calculatedBeamNames.Add(beam.Id, calculatedName);
            }
            //check to see if beam names already match
            Dictionary<string, bool> beamNameMatch = new Dictionary<string, bool>();
            foreach (var bn in beamNames)
            {
                if (bn.Value.Contains(calculatedBeamNames[bn.Key]))
                {
                    beamNameMatch.Add(bn.Key, true);
                }
                else
                {
                    beamNameMatch.Add(bn.Key, false);
                }
            }
            if (!beamNameMatch.All(bnm => bnm.Value))
            {
                plan.Course.Patient.BeginModifications();
                string message = "The following beam names do not match the naming convention:\n";
                foreach(var bnm in beamNameMatch)
                {
                    if (!bnm.Value)
                    {
                        message += $"Beam ID: {bnm.Key}, Current Name: {beamNames[bnm.Key]}, Suggested Name: {calculatedBeamNames[bnm.Key]}\n";
                        //testing automation in the approval Extension.
                        plan.Beams.First(b=>b.Id == bnm.Key).Name = calculatedBeamNames[bnm.Key];
                    }
                    //key is to add to the result.
                    result.Add(new PlanValidationResultDetail(plan.Id, message,
                        PlanValidationResultDetail.ResultClassification.ValidationWarning, "GS001"));

                }
            }
            return result;
        }

        public void PostPromote(PlanSetupApprovalStatus newStatus, IEnumerable<PlanSetup> plans /*, PlanValidationResult results */)
        {
            // TODO: Add here the code that is executed after the Eclipse Planning Approval wizard has been successfully completed.
        }
        private string GetProperBeamName(PlanSetup plan, Beam beam)
        {
            string beamName = plan.RTPrescription == null ? String.Empty : plan.RTPrescription.Site + " ";
            var orientation = plan.TreatmentOrientation;
            string directionPrefix = GetBeamDirPrefix(orientation, beam);
            if (!String.IsNullOrEmpty(directionPrefix))
            {
                beamName = directionPrefix + " ";
            }
            if (beam.Technique.Id.Contains("ARC"))
            {
                beamName += $"{beam.ControlPoints.First().GantryAngle} " +
                    $"{(beam.GantryDirection == GantryDirection.Clockwise ? "CW" : "CCW")} " +
                    $"{beam.ControlPoints.Last().GantryAngle} ";
            }
            else
            {
                //static beam
                beamName += $"{beam.ControlPoints.First().GantryAngle} ";
            }
            beamName += $"C{beam.ControlPoints.First().CollimatorAngle}";
            if (beam.ControlPoints.First().PatientSupportAngle != 0)
            {
                beamName += $" T{beam.ControlPoints.First().PatientSupportAngle}";
            }
            return beamName;
        }

        private string GetBeamDirPrefix(PatientOrientation orientation, Beam beam)
        {
            if (beam.Technique.Id.Contains("ARC")) { return String.Empty; }

            // Normalize angle to handle negative values and values over 360
            double gantry = beam.ControlPoints.First().GantryAngle;
            string dir = "";

            switch (orientation)
            {
                case PatientOrientation.HeadFirstSupine:
                    if (gantry == 0) dir = "AP";
                    else if (gantry > 0 && gantry < 90) dir = "LAO";  // Changed from RAO to LAO
                    else if (gantry == 90) dir = "LLAT";
                    else if (gantry > 90 && gantry < 180) dir = "LPO";
                    else if (gantry == 180) dir = "PA";
                    else if (gantry > 180 && gantry < 270) dir = "RPO";  // Changed from LPO to RPO for symmetry
                    else if (gantry == 270) dir = "RLAT";
                    else if (gantry > 270 && gantry < 360) dir = "RAO";  // This was already RAO
                    break;

                case PatientOrientation.HeadFirstProne:
                    if (gantry == 0) dir = "PA";
                    else if (gantry > 0 && gantry < 90) dir = "RPO";
                    else if (gantry == 90) dir = "RLAT";
                    else if (gantry > 90 && gantry < 180) dir = "RAO";
                    else if (gantry == 180) dir = "AP";
                    else if (gantry > 180 && gantry < 270) dir = "LAO";
                    else if (gantry == 270) dir = "LLAT";
                    else if (gantry > 270 && gantry < 360) dir = "LPO";
                    break;

                case PatientOrientation.FeetFirstSupine:
                    if (gantry == 0) dir = "PA";
                    else if (gantry > 0 && gantry < 90) dir = "RAO";
                    else if (gantry == 90) dir = "RLAT";
                    else if (gantry > 90 && gantry < 180) dir = "RPO";
                    else if (gantry == 180) dir = "AP";
                    else if (gantry > 180 && gantry < 270) dir = "LPO";
                    else if (gantry == 270) dir = "LLAT";
                    else if (gantry > 270 && gantry < 360) dir = "LAO";
                    break;

                case PatientOrientation.FeetFirstProne:
                    if (gantry == 0) dir = "AP";
                    else if (gantry > 0 && gantry < 90) dir = "LPO";
                    else if (gantry == 90) dir = "LLAT";
                    else if (gantry > 90 && gantry < 180) dir = "LAO";
                    else if (gantry == 180) dir = "PA";
                    else if (gantry > 180 && gantry < 270) dir = "RAO";
                    else if (gantry == 270) dir = "RLAT";
                    else if (gantry > 270 && gantry < 360) dir = "RPO";
                    break;
            }

            return dir;
        }
    }
}
