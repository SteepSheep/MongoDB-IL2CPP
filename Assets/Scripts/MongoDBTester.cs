using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using UnityEngine;

/// <summary>
/// Manager class for interactions with the MongoDB.
/// </summary>
[DisallowMultipleComponent]
public class MongoDBTester : MonoBehaviour
{
    public const string MONGODB_URI_FORMAT = "mongodb://{0}:{1}@{2}:{3}/{4}";
    private const string HOST_URI = "localhost";
    private const int PORT = 27017;
    private const string USER = "username";
    private const string PWD = "password";
    private const string AUTH_DB = "admin";
    private const string TEST_DB = "test";
    private const string TEST_COLLECTION = "collection";
    private const string TEST_FS = "files";

    private MongoClient client;
    private IMongoDatabase database;
    private IMongoCollection<TestData> testCollection;
    private GridFSBucket files;

    #region MonoBehaviour Functions

    private void Awake()
    {
        /*NOTE: MongoDB usually auto-maps classes as needed. In some cases it is necessary to manually register the class maps.
         * I haven't fully figured out in which situation it happens, but for me it only happened with my own classes used in collections.
        BsonClassMap.RegisterClassMap<ListData>();
        BsonClassMap.RegisterClassMap<DictionaryData>();
        */
    }

    private void Start()
    {
        InitConnection();
        TestMongoDB("test");
    }

    #endregion MonoBehaviour Functions

    #region Private Functions

    private void InitConnection()
    {
        client = new MongoClient(GetConnectionString(GetIPv4Host(HOST_URI), PORT, USER, PWD, AUTH_DB));
        Debug.Log("Got Client");
        database = client.GetDatabase(TEST_DB);
        testCollection = database.GetCollection<TestData>(TEST_COLLECTION);
        files = new GridFSBucket(database, new GridFSBucketOptions { BucketName = TEST_FS });
    }

    private async void TestMongoDB(string testName)
    {
        try
        {
            var insert = TestData.Random(testName);
            ShowMessage("Try uploading data: " + insert.ToBsonDocument());
            await testCollection.InsertOneAsync(insert);
            ShowMessage("Success");
            var replace = TestData.Random(testName);
            var replaceFilter = Builders<TestData>.Filter.Eq((t) => t.name, replace.name);
            ShowMessage("Try replacing file with: " + replace.ToBsonDocument());
            var res = await testCollection.ReplaceOneAsync(replaceFilter, replace);
            ShowMessage("Replacement was acknowledged: " + res.IsAcknowledged);
            var findFilter = Builders<TestData>.Filter.Eq((t) => t.name, replace.name);
            ShowMessage("Try finding data with name: " + replace.name);
            var find = await testCollection.Find(findFilter).FirstOrDefaultAsync();
            ShowMessage("Found data: " + find.ToBsonDocument());
            var del = await testCollection.DeleteOneAsync(findFilter);
            ShowMessage("Deleted data: " + del.IsAcknowledged);

            var texture = new Texture2D(16, 16);
            for (var i = 0; i < 16 * 16; i++)
            {
                texture.SetPixel(i % 16, i / 16, new Color(Random.value, Random.value, Random.value));
            }
            texture.Apply();
            var png = texture.EncodeToPNG();
            ShowMessage("Try uploading random texture");
            var uploadID = await files.UploadFromBytesAsync(testName, png);
            var fileFilter = Builders<GridFSFileInfo>.Filter.Eq((f) => f.Filename, testName);
            ShowMessage("Finding uploaded texture");
            var findFile = await files.Find(fileFilter).FirstOrDefaultAsync();
            if (findFile != null)
            {
                ShowMessage("Downloading texture again");
                var pngFile = await files.DownloadAsBytesAsync(findFile.Id);
                var loaded = texture.LoadImage(pngFile);
                ShowMessage("Loaded image data: " + loaded);
                var loadedSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
                FindObjectOfType<SpriteRenderer>().sprite = loadedSprite;
                ShowMessage("Deleting texture from server");
                await files.DeleteAsync(findFile.Id);
            }
        }
        catch (System.Exception e)
        {
            ShowMessage($"Test failed, exception: {e}");
        }

        void ShowMessage(string message)
        {
            Debug.Log(message);
            DebugText.Instance.AppendLine(message);
        }
    }

    #endregion Private Functions

    #region MongoDB Util

    //NOTE: I got an address family not supported exception. Not sure if this happens because of MongoDB, IL2CPP or the server where my MongoDB Instance was running, but this workaround helped in my case
    private string GetIPv4Host(string uri)
    {
        var adresses = System.Net.Dns.GetHostEntry(uri).AddressList;
        foreach (var adress in adresses)
        {
            if (adress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return adress.ToString();
            }
        }
        Debug.LogError($"{uri} is not a valid uri.");
        return null;
    }

    private string GetConnectionString(string host, int port, string user, string password, string defaultDB) => string.Format(MONGODB_URI_FORMAT, user, password, host, port, defaultDB);

    #endregion MongoDB Util
}