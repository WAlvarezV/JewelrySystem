USE PomonaDb
GO

SELECT * 
FROM Watches wat
LEFT JOIN Items itm ON itm.Id = wat.ItemId
LEFT JOIN Brands brd ON brd.Id = wat.BrandId
LEFT JOIN Persons prv ON prv.Id = itm.ProviderId