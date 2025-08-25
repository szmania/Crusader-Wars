using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.unit_mapper;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace CrusaderWars.terrain
{
    public static class Lands
    {
        public struct BattleMaps
        {
            public static (string X, string Y)[] Desert_Tiles = new[]
                   {
                            ("0.192","0.710"),
                            ("0.199","0.710"),
                            ("0.206","0.715"),
                            ("0.215","0.713"),
                            ("0.222","0.720"),
                            ("0.211", "0.676"),
                            ("0.186", "0.696"),
                            ("0.170", "0.697"),
                            ("0.225", "0.729"),
                            ("0.230", "0.729"),
                            ("0.285", "0.770"),
                            ("0.339", "0.775"),
                            ("0.327", "0.780"),
                            ("0.296", "0.773"),
                            ("0.380", "0.775"),
                            ("0.344", "0.752"),
                            ("0.344", "0.757"),
                            ("0.351", "0.761"),
                            ("0.363", "0.762"),
                            ("0.377", "0.761"),
                            ("0.382", "0.758"),
                            ("0.385", "0.754"),
                            ("0.352", "0.745"),
                            ("0.405", "0.781"),
                            ("0.407", "0.781"),
                            ("0.444", "0.766"),
                            ("0.451", "0.768"),
                            ("0.457", "0.764"),
                            ("0.464", "0.763"),
                            ("0.493", "0.756"),
                            ("0.453", "0.731"),
                            ("0.442", "0.738"),
                            ("0.435", "0.736"),
                            ("0.503", "0.785"),
                            ("0.512", "0.792"),
                            ("0.519", "0.794"),
                            ("0.443", "0.721"),
                            ("0.731", "0.755"),
                            ("0.731", "0.764"),
                            ("0.726", "0.784"),
                            ("0.729", "0.799"),
                            ("0.763", "0.821"),
                            ("0.759", "0.842"),
                            ("0.755", "0.855"),
                            ("0.784", "0.821"),
                            ("0.789", "0.819"),
                            ("0.668", "0.754"),
                            ("0.675", "0.749"),
                            ("0.652", "0.755"),
                            ("0.650", "0.768")
                     };

            public static(string X, string Y, string[] attPositions, string[] defPositions)[] DesertMountains_Tiles = new[]
                    {
                            ("0.107", "0.697", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.116", "0.695", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.117", "0.684", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.112", "0.683", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.125", "0.692", new string[]{"All", "All"}, new string[]{"All", "All"}),
                            ("0.131", "0.685", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.136", "0.682", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.140", "0.680", new string[]{"All", "All"}, new string[]{"All", "All"}),
                            ("0.148", "0.676", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.152", "0.675", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.164", "0.679", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.163", "0.678", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.170", "0.678", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.178", "0.674", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.186", "0.665", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.191", "0.649", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.194", "0.648", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.199", "0.662", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.206", "0.654", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.211", "0.653", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.222", "0.657", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.227", "0.659", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.486", "0.732", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.494", "0.735", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.607", "0.849", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.614", "0.894", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.616", "0.901", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.619", "0.908", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.627", "0.922", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.649", "0.949", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.656", "0.945", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.672", "0.955", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.673", "0.962", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.661", "0.832", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.669", "0.828", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.694", "0.887", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.698", "0.892", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.700", "0.898", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.718", "0.911", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.733", "0.897", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.737", "0.885", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.755", "0.882", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.763", "0.881", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.825", "0.815", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.821", "0.736", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.831", "0.736", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.856", "0.755", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.862", "0.756", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.846", "0.631", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.825", "0.625", new string[]{"S", "W"}, new string[]{"N", "E"})
                 };

            public static(string X, string Y)[] Dryland_Tiles = new[]
                    {
                            ("0.100", "0.667"),
                            ("0.106", "0.656"),
                            ("0.114", "0.658"),
                            ("0.112", "0.668"),
                            ("0.122", "0.661"),
                            ("0.121", "0.660"),
                            ("0.123", "0.668"),
                            ("0.128", "0.661"),
                            ("0.626", "0.782"),
                            ("0.629", "0.793"),
                            ("0.628", "0.792"),
                            ("0.642", "0.809"),
                            ("0.164", "0.648"),
                            ("0.262", "0.616"),
                            ("0.272", "0.631"),
                            ("0.277", "0.634"),
                            ("0.273", "0.654"),
                            ("0.289", "0.695"),
                            ("0.292", "0.697"),
                            ("0.298", "0.700"),
                            ("0.462", "0.719"),
                            ("0.456", "0.725"),
                            ("0.450", "0.729"),
                            ("0.447", "0.729"),
                            ("0.443", "0.732"),
                            ("0.444", "0.726"),
                            ("0.443", "0.718"),
                            ("0.445", "0.716"),
                            ("0.447", "0.716"),
                            ("0.436", "0.732"),
                            ("0.431", "0.729"),
                            ("0.430", "0.728"),
                            ("0.437", "0.720"),
                            ("0.440", "0.723"),
                            ("0.442", "0.718"),
                            ("0.444", "0.719"),
                            ("0.445", "0.718"),
                            ("0.447", "0.719"),
                            ("0.431", "0.720"),
                            ("0.437", "0.724"),
                            ("0.443", "0.724"),
                            ("0.424", "0.723"),
                            ("0.419", "0.721"),
                            ("0.415", "0.726"),
                            ("0.414", "0.720"),
                            ("0.416", "0.735"),
                            ("0.449", "0.723"),
                            ("0.450", "0.720"),
                            ("0.449", "0.728"),
                            ("0.452", "0.730")
           };

            public static(string X, string Y)[] Farmlands_Tiles = new[]
           {
                            ("0.142", "0.307"),
                            ("0.294", "0.441"),
                            ("0.282", "0.150"),
                            ("0.137", "0.151"),
                            ("0.133", "0.159"),
                            ("0.327", "0.313"),
                            ("0.297", "0.311"),
                            ("0.377", "0.339"),
                            ("0.393", "0.305"),
                            ("0.399", "0.356"),
                            ("0.462", "0.402"),
                            ("0.147", "0.311"),
                            ("0.140", "0.312"),
                            ("0.165", "0.327"),
                            ("0.162", "0.427"),
                            ("0.162", "0.434"),
                            ("0.153", "0.439"),
                            ("0.211", "0.372"),
                            ("0.277", "0.390"),
                            ("0.269", "0.376"),
                            ("0.369", "0.334"),
                            ("0.491", "0.434"),
                            ("0.498", "0.430"),
                            ("0.498", "0.442"),
                            ("0.338", "0.221"),
                            ("0.352", "0.255"),
                            ("0.282", "0.261"),
                            ("0.281", "0.192"),
                            ("0.327", "0.313"),
                            ("0.342", "0.347"),
                            ("0.370", "0.375"),
                            ("0.375", "0.177"),
                            ("0.126", "0.537"),
                            ("0.146", "0.497"),
                            ("0.302", "0.390"),
                            ("0.289", "0.433"),
                            ("0.146", "0.177"),
                            ("0.172", "0.205"),
                            ("0.126", "0.227"),
                            ("0.106", "0.185"),
                            ("0.117", "0.116"),
                            ("0.325", "0.157"),
                            ("0.473", "0.192"),
                            ("0.325", "0.487"),
                            ("0.490", "0.337"),
                            ("0.462", "0.402"),
                            ("0.468", "0.459"),
                            ("0.398", "0.487"),
                            ("0.540", "0.526"),
                            ("0.599", "0.487")

            };

            public static(string X, string Y)[] Forest_Tiles = new[]
                           {
                            ("0.285", "0.290"),
                            ("0.474", "0.254"),
                            ("0.584", "0.291"),
                            ("0.437", "0.121"),
                            ("0.433", "0.274"),
                            ("0.551", "0.542"),
                            ("0.735", "0.263"),
                            ("0.702", "0.363"),
                            ("0.312", "0.589"),
                            ("0.366", "0.305"),
                            ("0.880", "0.318"),
                            ("0.269", "0.106"),
                            ("0.548", "0.212"),
                            ("0.368", "0.157"),
                            ("0.386", "0.176"),
                            ("0.358", "0.219"),
                            ("0.292", "0.333"),
                            ("0.227", "0.337"),
                            ("0.308", "0.405"),
                            ("0.137", "0.331"),
                            ("0.419", "0.157"),
                            ("0.485", "0.257"),
                            ("0.429", "0.203"),
                            ("0.422", "0.192"),
                            ("0.406", "0.189"),
                            ("0.416", "0.157"), 
                            ("0.406", "0.189"),
                            ("0.506", "0.276"),
                            ("0.555", "0.267"),
                            ("0.560", "0.281"),
                            ("0.615", "0.283"),
                            ("0.621", "0.290"),
                            ("0.625", "0.292"),
                            ("0.630", "0.303"),
                            ("0.270", "0.240"),
                            ("0.272", "0.240"),
                            ("0.280", "0.249"),
                            ("0.305", "0.208"),
                            ("0.309", "0.216"),
                            ("0.316", "0.219"),
                            ("0.318", "0.228"),
                            ("0.229", "0.256"),
                            ("0.221", "0.261"),
                            ("0.264", "0.209"),
                            ("0.261", "0.212"),
                            ("0.257", "0.214"),
                            ("0.275", "0.183"),
                            ("0.296", "0.183"),
                            ("0.312", "0.180"),
                            ("0.310", "0.216")
                        };
            public static(string X, string Y, string[] attPositions, string[] defPositions)[] Hills_Tiles = new[]
                        {
                            ("0.151", "0.314", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.130", "0.323", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.115", "0.322", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.148", "0.301", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.173", "0.308", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.155", "0.352", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.160", "0.384", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.177", "0.390", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.182", "0.388", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.183", "0.375", new string[]{"All", "All"}, new string[]{"All", "All"}),
                            ("0.177", "0.407", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.181", "0.419", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.177", "0.448", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.216", "0.359", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.275", "0.372", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.285", "0.385", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.434", "0.374", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.433", "0.363", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.415", "0.344", new string[]{"All", "All"}, new string[]{"All", "All"}),
                            ("0.450", "0.399", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.455", "0.401", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.454", "0.401", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.473", "0.406", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.478", "0.416", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.500", "0.395", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.499", "0.426", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.331", "0.215", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.348", "0.220", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.352", "0.217", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.605", "0.513", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.612", "0.512", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.617", "0.526", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.562", "0.565", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.560", "0.565", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.178", "0.363", new string[]{"All", "All"}, new string[]{"All", "All"}),
                            ("0.432", "0.299", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.533", "0.287", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.526", "0.284", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.526", "0.273", new string[]{"All", "All"}, new string[]{"All", "All"}),
                            ("0.563", "0.311", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.551", "0.318", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.414", "0.218", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.426", "0.483", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.446", "0.486", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.418", "0.456", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.413", "0.455", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.387", "0.423", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.381", "0.422", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.375", "0.418", new string[]{"S", "W"}, new string[]{"N", "E"})
                        };
            public static(string X, string Y, string[] attPositions, string[] defPositions)[] Mountains_Tiles = new[]
                        {
                            ("0.183", "0.453", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.183", "0.487", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.175", "0.493", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.183", "0.496", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.171", "0.479", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.174", "0.480", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.158", "0.458", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.241", "0.368", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.190", "0.421", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.284", "0.374", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.295", "0.381", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.324", "0.364", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.320", "0.372", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.325", "0.362", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.322", "0.320", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.351", "0.321", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.355", "0.320", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.353", "0.341", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.367", "0.336", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.444", "0.376", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.369", "0.233", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.552", "0.486", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.565", "0.486", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.568", "0.500", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.574", "0.475", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.578", "0.474", new string[]{"All", "All"}, new string[]{"All", "All"}),
                            ("0.581", "0.491", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.616", "0.538", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.411", "0.416", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.418", "0.271", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.426", "0.274", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.433", "0.245", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.419", "0.301", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.321", "0.370", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.461", "0.471", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.477", "0.473", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.489", "0.442", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.486", "0.442", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.563", "0.490", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.554", "0.494", new string[]{"All", "All"}, new string[]{"All", "All"}),
                            ("0.687", "0.428", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.696", "0.427", new string[]{"S", "E"}, new string[]{"N", "W"}),
                            ("0.715", "0.440", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.753", "0.455", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.792", "0.586", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.808", "0.595", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.709", "0.464", new string[]{"N", "E"}, new string[]{"S", "W"}),
                            ("0.703", "0.461", new string[]{"N", "W"}, new string[]{"S", "E"}),
                            ("0.721", "0.453", new string[]{"S", "W"}, new string[]{"N", "E"}),
                            ("0.729", "0.437", new string[]{"N", "E"}, new string[]{"S", "W"})
                        };
            public static(string X, string Y)[] Plains_Tiles = new[]
                           {
                            ("0.143", "0.302"),
                            ("0.159", "0.328"),
                            ("0.162", "0.359"),
                            ("0.161", "0.368"),
                            ("0.160", "0.367"),
                            ("0.148", "0.377"),
                            ("0.154", "0.384"),
                            ("0.165", "0.384"),
                            ("0.169", "0.397"),
                            ("0.173", "0.391"),
                            ("0.189", "0.371"),
                            ("0.150", "0.371"),
                            ("0.151", "0.423"),
                            ("0.156", "0.423"),
                            ("0.159", "0.424"),
                            ("0.146", "0.424"),
                            ("0.141", "0.425"),
                            ("0.209", "0.366"),
                            ("0.141", "0.443"),
                            ("0.178", "0.440"),
                            ("0.183", "0.447"),
                            ("0.279", "0.373"),
                            ("0.279", "0.384"),
                            ("0.272", "0.390"),
                            ("0.438", "0.362"),
                            ("0.413", "0.364"),
                            ("0.456", "0.411"),
                            ("0.464", "0.401"),
                            ("0.477", "0.404"),
                            ("0.485", "0.399"),
                            ("0.492", "0.396"),
                            ("0.580", "0.570"),
                            ("0.478", "0.319"),
                            ("0.205", "0.342"),
                            ("0.120", "0.236"),
                            ("0.204", "0.338"),
                            ("0.400", "0.419"),
                            ("0.143", "0.429"),
                            ("0.284", "0.220"),
                            ("0.285", "0.220"),
                            ("0.448", "0.361"),
                            ("0.354", "0.502"),
                            ("0.445", "0.363"),
                            ("0.294", "0.390"),
                            ("0.343", "0.233"),
                            ("0.274", "0.145"),
                            ("0.172", "0.204")
                        };
            public static(string X, string Y)[] Steppe_Tiles = new[]
                  {
                            ("0.605", "0.194"),
                            ("0.601", "0.194"),
                            ("0.595", "0.200"),
                            ("0.593", "0.205"),
                            ("0.593", "0.207"),
                            ("0.585", "0.206"),
                            ("0.613", "0.209"),
                            ("0.604", "0.179"),
                            ("0.602", "0.167"),
                            ("0.598", "0.169"),
                            ("0.599", "0.156"),
                            ("0.604", "0.155"),
                            ("0.609", "0.159"),
                            ("0.590", "0.156"),
                            ("0.585", "0.150"),
                            ("0.639", "0.202"),
                            ("0.636", "0.198"),
                            ("0.637", "0.198"),
                            ("0.631", "0.199"),
                            ("0.630", "0.206"),
                            ("0.623", "0.201"),
                            ("0.632", "0.181"),
                            ("0.686", "0.205"),
                            ("0.670", "0.240"),
                            ("0.640", "0.195"),
                            ("0.640", "0.199"),
                            ("0.662", "0.168"),
                            ("0.669", "0.175"),
                            ("0.674", "0.187"),
                            ("0.669", "0.202"),
                            ("0.669", "0.207"),
                            ("0.675", "0.209"),
                            ("0.680", "0.211"),
                            ("0.704", "0.190"),
                            ("0.708", "0.185"),
                            ("0.588", "0.160"),
                            ("0.591", "0.161"),
                            ("0.572", "0.152"),
                            ("0.553", "0.219"),
                            ("0.556", "0.219"),
                            ("0.609", "0.244"),
                            ("0.715", "0.241"),
                            ("0.786", "0.251"),
                            ("0.619", "0.308"),
                            ("0.623", "0.315"),
                            ("0.631", "0.315"),
                            ("0.604", "0.225"),
                            ("0.610", "0.224"),
                            ("0.611", "0.263"),
                            ("0.617", "0.259"),
              };

            public static(string X, string Y)[] Taiga_Tiles = new[]
                           {
                    ("0.335", "0.100"),
                    ("0.334", "0.100"),
                    ("0.332", "0.112"),
                    ("0.439", "0.100"),
                    ("0.441", "0.105"),
                    ("0.442", "0.107"),
                    ("0.440", "0.105"),
                    ("0.443", "0.121"),
                    ("0.440", "0.131"),
                    ("0.459", "0.101"),
                    ("0.473", "0.117"),
                    ("0.470", "0.118"),
                    ("0.458", "0.125"),
                    ("0.642", "0.173"),
                    ("0.647", "0.176"),
                    ("0.709", "0.155"),
                    ("0.749", "0.147"),
                    ("0.750", "0.145"),
                    ("0.464", "0.130"),
                    ("0.524", "0.164"),
                    ("0.526", "0.159"),
                    ("0.536", "0.160"),
                    ("0.542", "0.164"),
                    ("0.535", "0.169"),
                    ("0.536", "0.167"),
                    ("0.541", "0.182"),
                    ("0.540", "0.183"),
                    ("0.540", "0.186"),
                    ("0.591", "0.186"),
                    ("0.560", "0.203"),
                    ("0.540", "0.210"),
                    ("0.548", "0.212"),
                    ("0.663", "0.192"),
                    ("0.656", "0.187"),
                    ("0.664", "0.183"),
                    ("0.774", "0.188"),
                    ("0.777", "0.187"),
                    ("0.769", "0.183"),
                    ("0.771", "0.194"),
                    ("0.778", "0.193"),
                    ("0.780", "0.191"),
                    ("0.819", "0.239"),
                    ("0.818", "0.238"),
                    ("0.825", "0.249"),
                    ("0.812", "0.249"),
                    ("0.802", "0.255"),
                    ("0.801", "0.263"),
                    ("0.801", "0.267"),
                    ("0.872", "0.267"),
                    ("0.872", "0.265")
                        };

            public static(string X, string Y)[] Wetlands_Tiles = new[]
                           {
                    ("0.165", "0.192"),
                    ("0.168", "0.192"),
                    ("0.174", "0.192"),
                    ("0.166", "0.192"),
                    ("0.169", "0.194"),
                    ("0.197", "0.234"),
                    ("0.196", "0.236"),
                    ("0.216", "0.223"),
                    ("0.216", "0.227"),
                    ("0.247", "0.180"),
                    ("0.244", "0.183"),
                    ("0.246", "0.259"),
                    ("0.249", "0.267"),
                    ("0.224", "0.288"),
                    ("0.224", "0.289"),
                    ("0.225", "0.290"),
                    ("0.221", "0.281"),
                    ("0.188", "0.291"),
                    ("0.192", "0.298"),
                    ("0.162", "0.414"),
                    ("0.552", "0.336"),
                    ("0.561", "0.343"),
                    ("0.582", "0.347"),
                    ("0.578", "0.351"),
                    ("0.585", "0.343"),
                    ("0.681", "0.345"),
                    ("0.761", "0.366"),
                    ("0.768", "0.359"),
                    ("0.769", "0.357"),
                    ("0.763", "0.350"),
                    ("0.696", "0.267"),
                    ("0.687", "0.271"),
                    ("0.807", "0.247"),
                    ("0.808", "0.247"),
                    ("0.834", "0.222"),
                    ("0.833", "0.223"),
                    ("0.838", "0.230"),
                    ("0.859", "0.337"),
                    ("0.570", "0.218"),
                    ("0.571", "0.229"),
                    ("0.732", "0.313"),
                    ("0.770", "0.358"),
                    ("0.735", "0.225"),
                    ("0.736", "0.224"),
                    ("0.740", "0.230"),
                    ("0.741", "0.231"),
                    ("0.735", "0.226"),
                    ("0.694", "0.267"),
                    ("0.694", "0.270"),
                    ("0.336", "0.163")
                        };

            public static(string X, string Y)[] Floodplains_Tiles = new[]
                            {
                    ("0.541", "0.718"),
                    ("0.539", "0.713"),
                    ("0.547", "0.713"),
                    ("0.544", "0.737"),
                    ("0.543", "0.776"),
                    ("0.553", "0.775"),
                    ("0.538", "0.752"),
                    ("0.539", "0.750"),
                    ("0.551", "0.771"),
                    ("0.547", "0.766"),
                    ("0.549", "0.768"),
                    ("0.553", "0.775"),
                    ("0.559", "0.782"),
                    ("0.561", "0.785"),
                    ("0.562", "0.786")
                        };

            
        };

        public static (string X, string Y, string[] attackerPositions, string[] defenderPositions) GetBattleMap(string terrain)
        {
            string[] ALL = { "All", "All" };
            string[] defPositions;
            string[] attPositions;

            Random rnd = new Random();
            (string X, string Y) Coordinates;
            int random;


            //Get battle maps from unit mappers
            if (UnitMappers_BETA.Terrains != null)
            {
                var terrainItems = UnitMappers_BETA.Terrains.GetNormalMaps().Where(item => item.terrain == terrain).ToList();

                if (terrainItems.Count > 0)
                {
                    // Generate a random index to select a random lava item
                    int randomIndex = new Random().Next(0, terrainItems.Count);

                    // Get the randomly selected lava item
                    var randomTerrainItem = terrainItems[randomIndex];
                    Coordinates.X = randomTerrainItem.x;
                    Coordinates.Y = randomTerrainItem.y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                }
            }


            switch (terrain)
            {
                case "desert":
                    random = rnd.Next(0, BattleMaps.Desert_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Desert_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Desert_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "desert_mountains":
                    random = rnd.Next(0, BattleMaps.DesertMountains_Tiles.Length - 1);
                    defPositions = BattleMaps.DesertMountains_Tiles[random].defPositions;
                    attPositions = BattleMaps.DesertMountains_Tiles[random].attPositions;
                    Coordinates.X = BattleMaps.DesertMountains_Tiles[random].X;
                    Coordinates.Y = BattleMaps.DesertMountains_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);

                case "drylands":
                    random = rnd.Next(0, BattleMaps.Dryland_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Dryland_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Dryland_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);

                case "farmlands":
                    random = rnd.Next(0, BattleMaps.Farmlands_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Farmlands_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Farmlands_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "forest":
                    random = rnd.Next(0, BattleMaps.Forest_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Forest_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Forest_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "hills":
                    random = rnd.Next(0, BattleMaps.Hills_Tiles.Length - 1);
                    defPositions = BattleMaps.Hills_Tiles[random].defPositions;
                    attPositions = BattleMaps.Hills_Tiles[random].attPositions;
                    Coordinates.X = BattleMaps.Hills_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Hills_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);
                case "mountains":
                    random = rnd.Next(0, BattleMaps.Mountains_Tiles.Length - 1);
                    defPositions = BattleMaps.Mountains_Tiles[random].defPositions;
                    attPositions = BattleMaps.Mountains_Tiles[random].attPositions;
                    Coordinates.X = BattleMaps.Mountains_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Mountains_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);
                case "plains":
                    random = rnd.Next(0, BattleMaps.Plains_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Plains_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Plains_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "steppe":
                    random = rnd.Next(0, BattleMaps.Steppe_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Steppe_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Steppe_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "taiga":
                    random = rnd.Next(0, BattleMaps.Taiga_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Taiga_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Taiga_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "wetlands":
                    random = rnd.Next(0, BattleMaps.Wetlands_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Wetlands_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Wetlands_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "oasis":
                    // Desert Copy
                    random = rnd.Next(0, BattleMaps.Desert_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Desert_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Desert_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "jungle":
                    // Dryland Copy 
                    random = rnd.Next(0, BattleMaps.Dryland_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Dryland_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Dryland_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);
                case "floodplains":
                    random = rnd.Next(0, BattleMaps.Floodplains_Tiles.Length - 1);
                    Coordinates.X = BattleMaps.Floodplains_Tiles[random].X;
                    Coordinates.Y = BattleMaps.Floodplains_Tiles[random].Y;
                    return (Coordinates.X, Coordinates.Y, ALL, ALL);

            }



            return ("0.631", "0.199", ALL, ALL);

        }
    }
}
