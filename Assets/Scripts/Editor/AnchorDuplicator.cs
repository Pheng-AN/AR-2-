// <copyright file="AnchorDuplicator.cs" company="Google LLC">
//
// Copyright 2024 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Google.XR.ARCoreExtensions.Codelabs.GeospatialCreatorApi
{
    using Google.XR.ARCoreExtensions.GeospatialCreator;

    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;

    public class AnchorDuplicator
    {
        private const string API_KEY = "AIzaSyDM40IfkgYse_aQPd-JMmGrkaSl2eUhDws";

        private static UnityWebRequest _placesApiRequest = null;
        private static PlacesApiResponse.Place[] _places = null;

        [MenuItem("Google AR Codelab/Run Places Request", false, 30)]
        public static void SearchForPlaces()
        {
            ARGeospatialCreatorOrigin origin =
                GameObject.FindObjectOfType<ARGeospatialCreatorOrigin>();
            if (origin == null)
            {
                Debug.LogError("No ARGeospatialCreatorOrigin exists in the scene.");
                return;
            }

            // cancel any in-progress request
            if (_placesApiRequest != null)
            {
                _placesApiRequest.Abort();
                _placesApiRequest.Dispose();
                _placesApiRequest = null;
            }

            _placesApiRequest = CreatePlacesRequest(
                API_KEY,
                "San Francisco Public Library",
                origin.Latitude,
                origin.Longitude
            );

            origin.StartCoroutine(SendPlacesRequest());
        }

        private static UnityWebRequest CreatePlacesRequest(
            string apiKey,
            string searchTerm,
            double lat,
            double lon
        )
        {
                    string postBody = "{ \"textQuery\": \"" + searchTerm + "\", " + 
                            "   \"locationBias\": { \"circle\": { " +
                            "      \"center\": { \"latitude\": " + lat + ", \"longitude\": " + lon + " }, " +
                            "      \"radius\": 10000 }" +
                            "   }" +
                            "}";

        string url = "https://places.googleapis.com/v1/places:searchText";

        UnityWebRequest request = UnityWebRequest.Post(url, postBody, "application/json");
        request.SetRequestHeader("X-Goog-Api-Key", apiKey);
        request.SetRequestHeader("X-Goog-FieldMask", "places.displayName,places.location");

        return request;
        }

        [MenuItem("Google AR Codelab/Create New Anchors from Places Response", false, 31)]
        public static void CreateNewAnchorsFromPlaces()
        {
                    if (_places == null)
        {
            Debug.LogError("Cannot create anchors: Places has not been initialized.");
            return;
        }

        // You start with only one anchor in the scene, which you want to copy:
        var prototypeAnchorObject = GameObject
            .FindObjectOfType<ARGeospatialCreatorAnchor>()
            .gameObject;

        foreach (var place in _places)
        {
            var newAnchorObject = GameObject.Instantiate(prototypeAnchorObject);
            var anchor = newAnchorObject.GetComponent<ARGeospatialCreatorAnchor>();
            anchor.Latitude = place.location.latitude;
            anchor.Longitude = place.location.longitude;

            newAnchorObject.name = place.displayName.text;
        }
        }

        private static IEnumerator SendPlacesRequest()
        {
            yield return _placesApiRequest.SendWebRequest();

            if (_placesApiRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(_placesApiRequest.error + ": " + _placesApiRequest.result);
            }
            else
            {
                _places = JsonUtility
                    .FromJson<PlacesApiResponse>(_placesApiRequest.downloadHandler.text)
                    .places;
                Debug.Log($"Request complete, {_places.Length} results found:");
                foreach (PlacesApiResponse.Place place in _places)
                {
                    Debug.Log(
                        $"\n{place.displayName.text} ({place.location.latitude}, {place.location.longitude})"
                    );
                }
            }

            // Clean up the request
            _placesApiRequest.Dispose();
            _placesApiRequest = null;
        }

        private class PlacesApiResponse
        {
            public Place[] places = null;
            public string status = null;

            [System.Serializable]
            internal class Place
            {
                public DisplayName displayName = null;
                public Location location = null;
            }

            [System.Serializable]
            internal class DisplayName
            {
                public string text = null;
                public string languageCode = null;
            }

            [System.Serializable]
            internal class Location
            {
                public double latitude = 0.0;
                public double longitude = 0.0;

                public Location(double lat, double lng)
                {
                    latitude = lat;
                    longitude = lng;
                }
            }
        }
    }
}
