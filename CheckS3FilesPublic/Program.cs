using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Net;

namespace CheckS3FilesPublic
{
    class Program
    {
        //Put in bucket name here
        static string bucketName = "";

        //Put in public URI to file here:
        static string pubURI = "";

        //Put in your keys for s3
        static AmazonS3Client s3Client = new AmazonS3Client("", "", Amazon.RegionEndpoint.EUCentral1);

        static int errorFiles = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Start Time: " + DateTime.Now.ToLongTimeString());
            ListBucketsContent();
            Console.WriteLine("End Time: " + DateTime.Now.ToLongTimeString());
        }

        static void ListBucketsContent()
        {
            int filesChecked = 0;
            ListObjectsRequest request = new ListObjectsRequest();
            request.BucketName = bucketName;
            ConsoleSpiner spin = new ConsoleSpiner();
            Console.Write("Working....");
            do
            {
                ListObjectsResponse response = s3Client.ListObjects(request);
                foreach (S3Object o in response.S3Objects)
                {
                    spin.Turn();
                    //Console.WriteLine("{0}\t{1}\t{2}", o.Key, o.Size, o.LastModified);
                    checkPublic(o.Key);
                    filesChecked++;
                }

                if (response.IsTruncated)
                {
                    request.Marker = response.NextMarker;
                }
                else
                {
                    request = null;
                }

            } while (request != null);

            Console.WriteLine("Finished Check");
            Console.WriteLine("\tFiles Checked: " + filesChecked);
            Console.WriteLine("\tnon Public Files: " + errorFiles);
        }

        static void checkPublic(string _key)
        {
            using (WebClient _webClient = new WebClient())
            {
                HttpWebResponse _response = null;

                try
                {
                    HttpWebRequest _request = (HttpWebRequest)HttpWebRequest.Create(pubURI + _key);
                    _request.Method = "GET";

                    _response = (HttpWebResponse)_request.GetResponse();
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.ProtocolError)
                    {
                        _response = (HttpWebResponse)e.Response;
                        Console.WriteLine("File: " + _key);
                        Console.WriteLine("\tErrorcode: {0}", _response.StatusCode);
                        errorFiles++;

                        if (_response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            MakePublic(_key);
                        }
                    }
                    else
                    {
                        Console.WriteLine("File: " + _key);
                        Console.WriteLine("\tError: {0}", e.Status);
                        errorFiles++;
                    }
                }
                finally
                {
                    if (_response != null)
                    {
                        _response.Close();
                    }
                }
            }
        }

        static void MakePublic(string _key)
        {
            try
            {
                PutACLRequest request = new PutACLRequest();
                request.BucketName = bucketName;
                request.Key = _key;
                request.CannedACL = S3CannedACL.PublicRead;
                s3Client.PutACL(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\t\t\tERROR! Could not make file: " + _key + " Public Read!");
                Console.WriteLine("\t\t\tMessage: " + ex.Message);
            }
        }
    }

    public class ConsoleSpiner
    {
        int counter;
        public ConsoleSpiner()
        {
            counter = 0;
        }
        public void Turn()
        {
            counter++;
            switch (counter % 4)
            {
                case 0: Console.Write("/"); break;
                case 1: Console.Write("-"); break;
                case 2: Console.Write("\\"); break;
                case 3: Console.Write("|"); break;
            }
            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
        }
    }
}
