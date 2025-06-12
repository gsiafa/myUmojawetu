var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    // Apply DataTable to all tables with the class 'datatable'
    $('.datatable').each(function () {
        dataTable = $(this).DataTable({
            lengthMenu: [[10, 20, 30, 40, 50, 100, -1], [10, 20, 30, 40, 50, 100, "All"]],
            pageLength: 10,
            sPaginationType: "full_numbers",
            language: {
                sLengthMenu: "Show _MENU_",
                search: '',
                searchPlaceholder: "Search Record...",
                sInfo: "Displaying _START_ to _END_ of _TOTAL_ records",
            },
            aaSorting: [[0, "asc"]],

            columnDefs: [
                { targets: 0, type: 'string' } // Ensure string sorting for the "Name" column
            ]
        });
    });

    $.fn.dataTable.ext.errMode = 'none';

    // Redraw the table when there's a filter changed
    $('.date-range-filter').change(function () {
        dataTable.draw();
    });
}
