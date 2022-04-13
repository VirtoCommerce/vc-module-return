
# Return Module Overview
The Return module by Virto Commerce gives you as an ecommerce website or online store admin an opportunity to view and manage all return operations performed by your customers in a single pane of glass. Once a customer returns an item to your store, this information will appear on the Return screen, where you can view and sort the return list at your convenience.
***Please note:*** *The Return module is still under development, which means some features are not accessible at the moment and will be available later. Please also see the Important note in the Working with Return Module section.*

# Locating Return Module
To locate the Return module on the Virto Commerce home screen, find it on the left hand panel and click it:

![Locating Return module](media/01-locating-return-module.png) 

If you cannot see it on the panel, click *More* and find it in the list in there:

![Locating Return module in More menu](media/02-locating-return-module-in-more.png)

You can also star the Return module to favorite it; this was, you will always be able to access it from the left hand panel of the home screen.

# Working with Return Module
## Viewing returns list
As mentioned in the overview, the Return module supplies you with a list of all return operations your customers requested. Here is how its home screen may look like:

![Return list home screen](media/03-return-list-overview.png)

As you can see, there are default columns, such as Return Number, Order Number, Customer, etc. You can also add more columns, as well as remove any of which you do not need, by ticking or unticking them after clicking the three line button:

![Configuring return list columns](media/04-configuring-return-list-columns.png) 

You can also sort the return operations (both ascending and descending, if applicable) by simply clicking the appropriate column title. The screen capture below shows the items sorted by return number, ascending:

![Return list sorted by number, ascending](media/05-return-list-sorted-by-number-ascending.png)

Finally, you can use the search box to type a keyword or key phrase and thus filter only the relevant items. For instance, you can type *New* to view only the return operations that have the *New* status:

![Using the search feature: Return list items with the New status only](media/06-return-list-search-new-only.png) 

***Important:*** *You can use both sorting and searching/filtering for all columns, except these three: Order Number, Customer, and Item Count. This is because these columns are borrowed from the Order module and thus are not native to the Return module.*

## Creating return from the list
There are two ways to create a return. The first is creation from the returns list with the "Add new return" button on top of it.

![Add new return button](media/07-add-new-return-button.png)

This button opens a blade with orders. Click on a order opens next blade with order line items. Here you can select line items to be returned, enter returning reason and quantity and optionally change the price. "Make return" button becames active when at least one line item with non-zero quantity is selected. Qunatity field has the validation. You cannot return more items than order contais and wich weren't been returned in oter returns related to this order. Return reason is optional.

![Order selection](media/08-order-select.png)

## Creating return from the order
The second way to create a return is using "Create return" button on top of the order blade. Click on this button opens a blade for line items selection to be return. It is the same blade as in the section above and works identically.

![Return creation from order](media/09-return-from-order.png)

## Return editing
You can edit return by click on it in the list. Also just created returns are opened for editing automatically. On the main return blade you can edit status and reason. Possible statuses list is editable. The list of line items can be opened by click on the widget with items count and it's total price on the bottom of the blade. It works similar to the creating process, expect absence of the checkboxes and impossibility of entering zero quantity. You cannot add or remove line items for a existing return.

![Return editing](media/10-return-editign.png)

## Related returns
The order blade contains the widget with returns count related to this order. Click on it opens a blade with a list of these returns.

![Related returns](media/11-related-returns.png)

# Process diagram
The return livetime can be described ty the following schema.

![Process diagram](media/12-process-diagram.png)
