﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.0.30319.33440.
// 
namespace aircraftreport {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class response {
        
        private int request_indexField;
        
        private data_source data_sourceField;
        
        private request requestField;
        
        private errors errorsField;
        
        private warnings warningsField;
        
        private int time_taken_msField;
        
        private data dataField;
        
        private string versionField;
        
        public response() {
            this.versionField = "1.0";
        }
        
        /// <remarks/>
        public int request_index {
            get {
                return this.request_indexField;
            }
            set {
                this.request_indexField = value;
            }
        }
        
        /// <remarks/>
        public data_source data_source {
            get {
                return this.data_sourceField;
            }
            set {
                this.data_sourceField = value;
            }
        }
        
        /// <remarks/>
        public request request {
            get {
                return this.requestField;
            }
            set {
                this.requestField = value;
            }
        }
        
        /// <remarks/>
        public errors errors {
            get {
                return this.errorsField;
            }
            set {
                this.errorsField = value;
            }
        }
        
        /// <remarks/>
        public warnings warnings {
            get {
                return this.warningsField;
            }
            set {
                this.warningsField = value;
            }
        }
        
        /// <remarks/>
        public int time_taken_ms {
            get {
                return this.time_taken_msField;
            }
            set {
                this.time_taken_msField = value;
            }
        }
        
        /// <remarks/>
        public data data {
            get {
                return this.dataField;
            }
            set {
                this.dataField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute("1.0")]
        public string version {
            get {
                return this.versionField;
            }
            set {
                this.versionField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class data_source {
        
        private string nameField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class request {
        
        private string typeField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type {
            get {
                return this.typeField;
            }
            set {
                this.typeField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class errors {
        
        private string errorField;
        
        /// <remarks/>
        public string error {
            get {
                return this.errorField;
            }
            set {
                this.errorField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class warnings {
        
        private string warningField;
        
        /// <remarks/>
        public string warning {
            get {
                return this.warningField;
            }
            set {
                this.warningField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class data {
        
        private AircraftReport[] aircraftReportField;
        
        private int num_resultsField;
        
        private bool num_resultsFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("AircraftReport")]
        public AircraftReport[] AircraftReport {
            get {
                return this.aircraftReportField;
            }
            set {
                this.aircraftReportField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int num_results {
            get {
                return this.num_resultsField;
            }
            set {
                this.num_resultsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool num_resultsSpecified {
            get {
                return this.num_resultsFieldSpecified;
            }
            set {
                this.num_resultsFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class AircraftReport {
        
        private string receipt_timeField;
        
        private string observation_timeField;
        
        private quality_control_flags quality_control_flagsField;
        
        private string aircraft_refField;
        
        private float latitudeField;
        
        private bool latitudeFieldSpecified;
        
        private float longitudeField;
        
        private bool longitudeFieldSpecified;
        
        private int altitude_ft_mslField;
        
        private bool altitude_ft_mslFieldSpecified;
        
        private sky_condition[] sky_conditionField;
        
        private turbulence_condition[] turbulence_conditionField;
        
        private icing_condition[] icing_conditionField;
        
        private int visibility_statute_miField;
        
        private bool visibility_statute_miFieldSpecified;
        
        private string wx_stringField;
        
        private float temp_cField;
        
        private bool temp_cFieldSpecified;
        
        private int wind_dir_degreesField;
        
        private bool wind_dir_degreesFieldSpecified;
        
        private int wind_speed_ktField;
        
        private bool wind_speed_ktFieldSpecified;
        
        private int vert_gust_ktField;
        
        private bool vert_gust_ktFieldSpecified;
        
        private string report_typeField;
        
        private string raw_textField;
        
        /// <remarks/>
        public string receipt_time {
            get {
                return this.receipt_timeField;
            }
            set {
                this.receipt_timeField = value;
            }
        }
        
        /// <remarks/>
        public string observation_time {
            get {
                return this.observation_timeField;
            }
            set {
                this.observation_timeField = value;
            }
        }
        
        /// <remarks/>
        public quality_control_flags quality_control_flags {
            get {
                return this.quality_control_flagsField;
            }
            set {
                this.quality_control_flagsField = value;
            }
        }
        
        /// <remarks/>
        public string aircraft_ref {
            get {
                return this.aircraft_refField;
            }
            set {
                this.aircraft_refField = value;
            }
        }
        
        /// <remarks/>
        public float latitude {
            get {
                return this.latitudeField;
            }
            set {
                this.latitudeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool latitudeSpecified {
            get {
                return this.latitudeFieldSpecified;
            }
            set {
                this.latitudeFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public float longitude {
            get {
                return this.longitudeField;
            }
            set {
                this.longitudeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool longitudeSpecified {
            get {
                return this.longitudeFieldSpecified;
            }
            set {
                this.longitudeFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public int altitude_ft_msl {
            get {
                return this.altitude_ft_mslField;
            }
            set {
                this.altitude_ft_mslField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool altitude_ft_mslSpecified {
            get {
                return this.altitude_ft_mslFieldSpecified;
            }
            set {
                this.altitude_ft_mslFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("sky_condition")]
        public sky_condition[] sky_condition {
            get {
                return this.sky_conditionField;
            }
            set {
                this.sky_conditionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("turbulence_condition")]
        public turbulence_condition[] turbulence_condition {
            get {
                return this.turbulence_conditionField;
            }
            set {
                this.turbulence_conditionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("icing_condition")]
        public icing_condition[] icing_condition {
            get {
                return this.icing_conditionField;
            }
            set {
                this.icing_conditionField = value;
            }
        }
        
        /// <remarks/>
        public int visibility_statute_mi {
            get {
                return this.visibility_statute_miField;
            }
            set {
                this.visibility_statute_miField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool visibility_statute_miSpecified {
            get {
                return this.visibility_statute_miFieldSpecified;
            }
            set {
                this.visibility_statute_miFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public string wx_string {
            get {
                return this.wx_stringField;
            }
            set {
                this.wx_stringField = value;
            }
        }
        
        /// <remarks/>
        public float temp_c {
            get {
                return this.temp_cField;
            }
            set {
                this.temp_cField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool temp_cSpecified {
            get {
                return this.temp_cFieldSpecified;
            }
            set {
                this.temp_cFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public int wind_dir_degrees {
            get {
                return this.wind_dir_degreesField;
            }
            set {
                this.wind_dir_degreesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool wind_dir_degreesSpecified {
            get {
                return this.wind_dir_degreesFieldSpecified;
            }
            set {
                this.wind_dir_degreesFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public int wind_speed_kt {
            get {
                return this.wind_speed_ktField;
            }
            set {
                this.wind_speed_ktField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool wind_speed_ktSpecified {
            get {
                return this.wind_speed_ktFieldSpecified;
            }
            set {
                this.wind_speed_ktFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public int vert_gust_kt {
            get {
                return this.vert_gust_ktField;
            }
            set {
                this.vert_gust_ktField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool vert_gust_ktSpecified {
            get {
                return this.vert_gust_ktFieldSpecified;
            }
            set {
                this.vert_gust_ktFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public string report_type {
            get {
                return this.report_typeField;
            }
            set {
                this.report_typeField = value;
            }
        }
        
        /// <remarks/>
        public string raw_text {
            get {
                return this.raw_textField;
            }
            set {
                this.raw_textField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class quality_control_flags {
        
        private string mid_point_assumedField;
        
        private string no_time_stampField;
        
        private string flt_lvl_rangeField;
        
        private string above_ground_level_indicatedField;
        
        private string no_flt_lvlField;
        
        private string bad_locationField;
        
        /// <remarks/>
        public string mid_point_assumed {
            get {
                return this.mid_point_assumedField;
            }
            set {
                this.mid_point_assumedField = value;
            }
        }
        
        /// <remarks/>
        public string no_time_stamp {
            get {
                return this.no_time_stampField;
            }
            set {
                this.no_time_stampField = value;
            }
        }
        
        /// <remarks/>
        public string flt_lvl_range {
            get {
                return this.flt_lvl_rangeField;
            }
            set {
                this.flt_lvl_rangeField = value;
            }
        }
        
        /// <remarks/>
        public string above_ground_level_indicated {
            get {
                return this.above_ground_level_indicatedField;
            }
            set {
                this.above_ground_level_indicatedField = value;
            }
        }
        
        /// <remarks/>
        public string no_flt_lvl {
            get {
                return this.no_flt_lvlField;
            }
            set {
                this.no_flt_lvlField = value;
            }
        }
        
        /// <remarks/>
        public string bad_location {
            get {
                return this.bad_locationField;
            }
            set {
                this.bad_locationField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class sky_condition {
        
        private string sky_coverField;
        
        private int cloud_base_ft_mslField;
        
        private bool cloud_base_ft_mslFieldSpecified;
        
        private int cloud_top_ft_mslField;
        
        private bool cloud_top_ft_mslFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string sky_cover {
            get {
                return this.sky_coverField;
            }
            set {
                this.sky_coverField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int cloud_base_ft_msl {
            get {
                return this.cloud_base_ft_mslField;
            }
            set {
                this.cloud_base_ft_mslField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool cloud_base_ft_mslSpecified {
            get {
                return this.cloud_base_ft_mslFieldSpecified;
            }
            set {
                this.cloud_base_ft_mslFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int cloud_top_ft_msl {
            get {
                return this.cloud_top_ft_mslField;
            }
            set {
                this.cloud_top_ft_mslField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool cloud_top_ft_mslSpecified {
            get {
                return this.cloud_top_ft_mslFieldSpecified;
            }
            set {
                this.cloud_top_ft_mslFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class turbulence_condition {
        
        private string turbulence_typeField;
        
        private string turbulence_intensityField;
        
        private int turbulence_base_ft_mslField;
        
        private bool turbulence_base_ft_mslFieldSpecified;
        
        private int turbulence_top_ft_mslField;
        
        private bool turbulence_top_ft_mslFieldSpecified;
        
        private string turbulence_freqField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string turbulence_type {
            get {
                return this.turbulence_typeField;
            }
            set {
                this.turbulence_typeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string turbulence_intensity {
            get {
                return this.turbulence_intensityField;
            }
            set {
                this.turbulence_intensityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int turbulence_base_ft_msl {
            get {
                return this.turbulence_base_ft_mslField;
            }
            set {
                this.turbulence_base_ft_mslField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool turbulence_base_ft_mslSpecified {
            get {
                return this.turbulence_base_ft_mslFieldSpecified;
            }
            set {
                this.turbulence_base_ft_mslFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int turbulence_top_ft_msl {
            get {
                return this.turbulence_top_ft_mslField;
            }
            set {
                this.turbulence_top_ft_mslField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool turbulence_top_ft_mslSpecified {
            get {
                return this.turbulence_top_ft_mslFieldSpecified;
            }
            set {
                this.turbulence_top_ft_mslFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string turbulence_freq {
            get {
                return this.turbulence_freqField;
            }
            set {
                this.turbulence_freqField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class icing_condition {
        
        private string icing_typeField;
        
        private string icing_intensityField;
        
        private int icing_base_ft_mslField;
        
        private bool icing_base_ft_mslFieldSpecified;
        
        private int icing_top_ft_mslField;
        
        private bool icing_top_ft_mslFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string icing_type {
            get {
                return this.icing_typeField;
            }
            set {
                this.icing_typeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string icing_intensity {
            get {
                return this.icing_intensityField;
            }
            set {
                this.icing_intensityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int icing_base_ft_msl {
            get {
                return this.icing_base_ft_mslField;
            }
            set {
                this.icing_base_ft_mslField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool icing_base_ft_mslSpecified {
            get {
                return this.icing_base_ft_mslFieldSpecified;
            }
            set {
                this.icing_base_ft_mslFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int icing_top_ft_msl {
            get {
                return this.icing_top_ft_mslField;
            }
            set {
                this.icing_top_ft_mslField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool icing_top_ft_mslSpecified {
            get {
                return this.icing_top_ft_mslFieldSpecified;
            }
            set {
                this.icing_top_ft_mslFieldSpecified = value;
            }
        }
    }
}
