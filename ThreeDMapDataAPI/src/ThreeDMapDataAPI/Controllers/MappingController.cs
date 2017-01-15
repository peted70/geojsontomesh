using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OsmToGeoJSON;

namespace ThreeDMapDataAPI.Controllers
{
    public class MappingDto
    {
        public string GeoJSON { get; set; }
        public ImageDto ImageData { get; set; }
    }

    public class ImageDto
    {
        public byte[] Image { get; set; }
        public string Metadata { get; set; }
    }

    [Route("api/[controller]")]
    public class MappingController : Controller
    {
        private IHostingEnvironment _he;

        public MappingController(IHostingEnvironment he)
        {
            _he = he;
        }

        // http://localhost:8165/api/mapping?maxLon=51.5140574994&maxLat=-0.1145303249&minLon=51.5073134351&minLat=-0.1295164166
        [HttpGet]
        [ProducesResponseType(typeof(MappingDto), 200)]
        public async Task<ActionResult> Get(GeoRect rect)
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(ModelState);
            }

            var result = new MappingDto();
            var geoJSONTask = GetGeoJSON(rect);
            var imgTask = GetImageAsync(rect);
            var imgMetadataTask = GetImageMetadataAsync(rect);

            await Task.WhenAll(geoJSONTask, imgTask, imgMetadataTask);
            result.GeoJSON = geoJSONTask.Result;
            result.ImageData = new ImageDto
            {
                Image = imgTask.Result,
                Metadata = imgMetadataTask.Result
            };
            return Ok(result);
        }

        // http://localhost:8165/api/mapping/image?maxLon=51.5140574994&maxLat=-0.1145303249&minLon=51.5073134351&minLat=-0.1295164166
        [HttpGet]
        [Route("image")]
        [ProducesResponseType(typeof(byte[]), 200)]
        public async Task<ActionResult> GetImage(GeoRect rect)
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(ModelState);
            }
            var imgTask = await GetImageAsync(rect);
            return Ok(imgTask);
        }

        [HttpGet]
        [Route("api/metadata")]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<ActionResult> GetImageMetadata(GeoRect rect)
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(ModelState);
            }
            var str = await GetImageMetadataAsync(rect);
            return Ok(str);
        }

        //// GET api/values
        //// http://localhost:8165/api/mapping?maxLon=51.5140574994&maxLat=-0.1145303249&minLon=51.5073134351&minLat=-0.1295164166
        //[HttpGet]
        //public async Task<ActionResult> Get(GeoRect rect)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return new JsonResult(ModelState);
        //    }

        //    var httpStr = $"http://overpass-api.de/api/interpreter?data=[out:json];(node[\"building\"]({rect.MinLat},{rect.MinLon},{rect.MaxLat},{rect.MaxLon});way[\"building\"]({rect.MinLat},{rect.MinLon},{rect.MaxLat},{rect.MaxLon});relation[\"building\"]({rect.MinLat},{rect.MinLon},{rect.MaxLat},{rect.MaxLon}););out body;>;out skel qt;";
        //    var http = new HttpClient();
        //    var tsk = await http.GetAsync(httpStr);
        //    var strTsk = await tsk.Content.ReadAsStringAsync();

        //    Converter.FilesRoot = _he.WebRootPath + Path.DirectorySeparatorChar.ToString();
        //    var converter = new Converter();
        //    var geojson = converter.OsmToGeoJSON(strTsk);
        //    return Content(geojson);
        //}

        private async Task<string> GetGeoJSON(GeoRect rect)
        {
            var httpStr = $"http://overpass-api.de/api/interpreter?data=[out:json];(node[\"building\"]({rect.MinLat},{rect.MinLon},{rect.MaxLat},{rect.MaxLon});way[\"building\"]({rect.MinLat},{rect.MinLon},{rect.MaxLat},{rect.MaxLon});relation[\"building\"]({rect.MinLat},{rect.MinLon},{rect.MaxLat},{rect.MaxLon}););out body;>;out skel qt;";
            var http = new HttpClient();
            var tsk = await http.GetAsync(httpStr);
            var strTsk = await tsk.Content.ReadAsStringAsync();

            Converter.FilesRoot = _he.WebRootPath + Path.DirectorySeparatorChar.ToString();
            var converter = new Converter();
            var geojson = converter.OsmToGeoJSON(strTsk);
            return geojson;
        }

        public async Task<byte[]> GetImageAsync(GeoRect rect)
        {
            var bingKey = Environment.GetEnvironmentVariable("BING_MAPS_KEY");
            var httpStr = $"http://dev.virtualearth.net/REST/v1/Imagery/Map/Aerial?";
            httpStr += "mapArea ={rect.MaxLat},{rect.MinLon},{rect.MinLat},{rect.MaxLon}";
            httpStr += "&mapSize=1500,1500";
            httpStr += "&key={bingKey}";
            var http = new HttpClient();
            var tsk = await http.GetAsync(httpStr);
            var bytesTsk = await tsk.Content.ReadAsByteArrayAsync();
            return bytesTsk;
        }

        public async Task<string> GetImageMetadataAsync(GeoRect rect)
        {
            var bingKey = Environment.GetEnvironmentVariable("BING_MAPS_KEY");
            var httpStr = $"http://dev.virtualearth.net/REST/v1/Imagery/Map/Aerial?";
            httpStr += "mapArea ={rect.MaxLat},{rect.MinLon},{rect.MinLat},{rect.MaxLon}";
            httpStr += "&mapSize=1500,1500";
            httpStr += "&mapMetadata=1";
            httpStr += "&key={bingKey}";
            var http = new HttpClient();
            var tsk = await http.GetAsync(httpStr);
            var str = await tsk.Content.ReadAsStringAsync();
            return str;
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
