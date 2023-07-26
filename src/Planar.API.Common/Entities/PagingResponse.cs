using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Planar.API.Common.Entities
{
    public class PagingResponse<T> : PagingResponse
        where T : class
    {
        private List<T>? _data;

        public PagingResponse()
        {
        }

        public PagingResponse(List<T> data)
        {
            Data = data;
        }

        public PagingResponse(List<T> data, int totalRows)
        {
            Data = data;
            TotalRows = totalRows;
        }

        public PagingResponse(IPagingRequest request, List<T> response, int totalRows)
        {
            Data = response;
            SetPagingData(request);
            TotalRows = totalRows;
        }

        public PagingResponse(PagingResponse<T> pagingResponse)
        {
            Data = pagingResponse.Data;
            PageNumber = pagingResponse.PageNumber;
            PageSize = pagingResponse.PageSize;
            TotalRows = pagingResponse.TotalRows;
        }

        [JsonPropertyOrder(99)]
        public List<T>? Data
        {
            get { return _data; }
            set
            {
                _data = value;
                Count = _data?.Count ?? 0;
            }
        }
    }

    public class PagingResponse : IPagingResponse
    {
        private int _totalRows;

        public PagingResponse()
        {
        }

        public void SetPagingData(IPagingRequest request, int totalRows)
        {
            SetPagingData(request);
            TotalRows = totalRows;
        }

        public void SetPagingData(IPagingRequest request)
        {
            request.SetPagingDefaults();
            PageNumber = request.PageNumber.GetValueOrDefault();
            PageSize = request.PageSize.GetValueOrDefault();
        }

        [JsonPropertyOrder(1)]
        public int Count { get; set; }

        [JsonPropertyOrder(2)]
        public int PageNumber { get; set; }

        [JsonPropertyOrder(3)]
        public int PageSize { get; set; }

        [JsonPropertyOrder(4)]
        public int TotalRows
        {
            get { return _totalRows; }
            set
            {
                _totalRows = value;
                if (PageSize > 0 && _totalRows > 0)
                {
                    TotalPages = Convert.ToInt32(Math.Ceiling(_totalRows * 1.0 / PageSize));
                }
            }
        }

        [JsonPropertyOrder(5)]
        public int TotalPages { get; set; }

        public bool IsLastPage => PageNumber >= TotalPages;
    }
}