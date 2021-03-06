﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Catalog.Application.DbModels;
using EventBusAwsSns.Shared.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Infrastructure.Repositories
{
    public class CatalogRepository : ICatalogRepository
    {
        private readonly CatalogContext _context;
        private readonly ILogger _logger;
        
        public CatalogRepository(CatalogContext context, ILogger<CatalogRepository> logger)
        {
            _logger = logger;
            _context = context;
        }
        
        public IEnumerable<Product> GetAllProducts()
        {
            return _context.Products;
        }

        public Task<int> UpdateProductsAssetsAsync(List<OrderItemInfo> orderItemInfo)
        {
            foreach (var productInfo in orderItemInfo)
            {
                var product = _context.Products.Find(productInfo.ProductId);

                if (product == null)
                {
                    continue;
                }
                
                if (productInfo.Assets > product.Assets)
                {
                    _logger.LogError($"A order item exceed the availability of the product, " +
                                     $"available: {product.Assets}, requested: {productInfo.Assets}");
                    continue;
                }
                
                product.Assets -= productInfo.Assets;
            }
            
            return _context.SaveChangesAsync();
        }
    }
}