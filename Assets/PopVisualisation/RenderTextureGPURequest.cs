using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class RenderTextureGPURequest : MonoBehaviour
{
        public RenderTexture renderTexture;
         
        private Queue<AsyncGPUReadbackRequest> requests
            = new Queue<AsyncGPUReadbackRequest>();
     
        private float t;
        private float timeBetweenRequests = 0.2f;
         
        void Update() {
            // Handle Request Queue
            while (requests.Count > 0) {
                // Get the first Request in the Queue
                AsyncGPUReadbackRequest request = requests.Peek();
     
                if (request.hasError) {
                    // Error!
                    Debug.LogWarning("AsyncGPUReadbackRequest Error! :(");
                    requests.Dequeue(); // Remove from Queue
                } else if (request.done) {
                    // Request is done, Obtain data!
                   // NativeArray<Color32> data = request.GetData<Color32>();
                    // RGBA32 -> use Color32
                    // RGBAFloat -> use Color
                    // else, you may have to use the raw byte array:
                    // NativeArray<byte> data = request.GetData<byte>();
                     
                    // Do something with the data
                    // if (data.Length <= 0) {
                    //     // No data?
                    // } else if (data.Length == 1) {
                    //     // Single Pixel
                    //     // Note, we don't know the coords of the pixel obtained
                    //     // If you want this information, consider wrapping the
                    //     // AsyncGPUReadbackRequest object in a custom class.
                    // } else {
                    //     // Full Image
                    // }
                    requests.Dequeue(); // Remove from Queue
                } else {
                    // Request is still processing.
                    break;
                }
                // Note : We have to Dequeue items or break,
                // or we'll be caught in an infinite loop!
            }
     
            // Handle Request Timer
            // t += Time.deltaTime;
            // if (t > timeBetweenRequests) {
            //     t = 0;
            //     RequestScreen();
            //     //RequestPixel(0, 0); 
            //     // Note that 0,0 is in the bottom left corner
            //     // of the Render Texture
            // }
        }
         
        private void RequestScreen() {
            AsyncGPUReadbackRequest rq = AsyncGPUReadback.Request(
                renderTexture
            );
            requests.Enqueue(rq);
        }
     
        private void RequestPixel(int x, int y) {
            if (x < 0 || x >= renderTexture.width || 
                y < 0 || y > renderTexture.height) {
                // Pixel out of the render texture bounds!
                return;
            }
     
            AsyncGPUReadbackRequest rq = AsyncGPUReadback.Request(
                renderTexture, // Render Texture
                0, // Mip Map level
                Mathf.RoundToInt(x), // x
                1, // Width (1 as we want a single pixel)
                Mathf.RoundToInt(y), // y
                1, // Height (1 as we want a single pixel)
                0, // z
                1, // Depth
                TextureFormat.RGBA32); // Format
            // I believe this should reflect the Color Format the render texture has,
            // 8 bits per channel = RGBA32 (or R8, RG16, RGB24)
            // 16 bits per channel = RGBAHalf (or RHalf, RGHalf, RGBHalf)
            // 32 bits per channel = RGBAFloat (or RFloat, RGFloat, RGBFloat)
            // Note that not all Color Formats are supported by AsyncGPUReadback
            // Some will return errors/warnings.
            // UNORM seems to be supported, but SNORM returns errors.
            // If you need negative values, use a Half or Float format
            // I recommend using RGBA32 or RGBAFloat, as you can retrieve
            // the data as a NativeArray of Color32 or Color objects respectively.
     
            requests.Enqueue(rq);
        }
        

        public void RequestPixelData(RenderTexture rt, Action<AsyncGPUReadbackRequest> requestComplete)
        {
            AsyncGPUReadbackRequest rq = AsyncGPUReadback.Request(
                rt, // Render Texture
                0, 
                0, 
                rt.width, // Width (1 as we want a single pixel)
                0, // y
                rt.height, // Height (1 as we want a single pixel)
                0, // z
                1, // Depth
                TextureFormat.RGBA32, requestComplete);
            
            requests.Enqueue(rq);
        }
}
