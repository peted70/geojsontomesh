using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OsmToGeoJSON;

namespace ThreeDMapDataAPI.Controllers
{
    [Route("api/[controller]")]
    public class MappingController : Controller
    {
        private IHostingEnvironment _he;

        public MappingController(IHostingEnvironment he)
        {
            _he = he;
        }

        // GET api/values
        // http://localhost:8165/api/mapping?maxLon=51.5140574994&maxLat=-0.1145303249&minLon=51.5073134351&minLat=-0.1295164166
        [HttpGet]
        public async Task<string> Get(GeoRect rect)
        {
            if (!ModelState.IsValid)
                return null;

            var httpStr = $"http://overpass-api.de/api/interpreter?data=[out:json];(node[\"building\"]({rect.MinLon},{rect.MinLat},{rect.MaxLon},{rect.MaxLat});way[\"building\"]({rect.MinLon},{rect.MinLat},{rect.MaxLon},{rect.MaxLat});relation[\"building\"]({rect.MinLon},{rect.MinLat},{rect.MaxLon},{rect.MaxLat}););out body;>;out skel qt;";
            var http = new HttpClient();
            var tsk = await http.GetAsync(httpStr);
            var strTsk = await tsk.Content.ReadAsStringAsync();

            Converter.FilesRoot = _he.WebRootPath + Path.DirectorySeparatorChar.ToString();
            var converter = new Converter();
            var geojson = converter.OsmToGeoJSON(strTsk);
            return geojson;
        }
    }

    public class GeoRect
    {
        [BindRequired]
        public float MaxLon { get; set; }
        [BindRequired]
        public float MaxLat { get; set; }
        [BindRequired]
        public float MinLon { get; set; }
        [BindRequired]
        public float MinLat { get; set; }
    }
}
