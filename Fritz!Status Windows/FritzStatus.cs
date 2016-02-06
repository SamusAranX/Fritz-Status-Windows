using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Fritz_Status {
	enum FritzConnectionStatus {
		Connected,
		Training,
		NotConnected,
		Unknown
	}

	public class DslOverview {
		public string dslam { get; set; }
		public Line[] line { get; set; }
		public string ds_rate { get; set; }
		public string us_rate { get; set; }
	}

	public class Line {
		public string mode { get; set; }
		public string train_state { get; set; }
		public string time { get; set; }
		public string pic { get; set; } // FRITZ!OS < 6.50 ?
		public string state { get; set; } // FRITZ!OS >= 6.50 ?
	}

	public struct BoxNameAndOS {
		public string BoxName;
		public string BoxOS;
	}


	static class FritzStatus {
		const string FRITZ_HOME = "http://fritz.box/home/home.lua";
		const string FRITZ_FBOS = "http://fritz.box/home/pp_fbos.lua";
		const string FRITZ_OVERVIEW = "http://fritz.box/internet/dsl_overview.lua?useajax=1&action=get_data";

		const string SID_REGEX = "\\?sid=(.*)\"";
		const string BOXNAME_REGEX = "\\[\"PRODUKT_NAME\\\"\\] = \"(.*)\"";
		const string BOXNAME2_REGEX = "<div>\n<p>(.*)\n";
		const string BOX_OS_REGEX = "</p>\n<p>(.*)\n";

		static Regex sidRegex = new Regex(SID_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		static Regex boxNameRegex = new Regex(BOXNAME_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		static Regex boxName2Regex = new Regex(BOXNAME2_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		static Regex boxOSRegex = new Regex(BOX_OS_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public static async Task<DslOverview> GetStatus() {
			HttpWebRequest request = WebRequest.CreateHttp(FRITZ_OVERVIEW);
			request.Timeout = 5000;

			try {
				HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
					string responseText = reader.ReadToEnd();

					JavaScriptSerializer jss = new JavaScriptSerializer();
					DslOverview overview = jss.Deserialize<DslOverview>(responseText);

					return overview;
				}
			} catch (Exception ex) {
				Debug.WriteLine(ex);
				return null;
			}
		}

		public static async Task<string> GetBoxName() {
			HttpWebRequest request = WebRequest.CreateHttp(FRITZ_HOME);
			request.Timeout = 15000;

			try {
				HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
					string responseText = reader.ReadToEnd();

					return boxNameRegex.Match(responseText).Groups[1].Value;
				}
			} catch (Exception ex) {
				Debug.WriteLine(ex);
				return null;
			}
		}

		public static async Task<BoxNameAndOS> GetBoxNameAndOS() {
			HttpWebRequest request = WebRequest.CreateHttp(FRITZ_FBOS);
			request.Timeout = 10000;

			try {
				HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
					string responseText = reader.ReadToEnd();

					return new BoxNameAndOS() {
						BoxName = boxName2Regex.Match(responseText).Groups[1].Value,
						BoxOS = boxOSRegex.Match(responseText).Groups[1].Value
					};
				}
			} catch (Exception ex) {
				Debug.WriteLine(ex);
				return new BoxNameAndOS();
			}
		}


	}
}
