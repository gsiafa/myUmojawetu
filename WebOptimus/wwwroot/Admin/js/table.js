var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('.myTable').DataTable({
        lengthMenu: [[10, 20, 30, 40, 50, 100, -1], [10, 20, 30, 40, 50, 100, "All"]],
        pageLength: 10,
        sPaginationType: "full_numbers",
        scrollX: true, // Enable horizontal scrolling
        responsive: {
            details: false, // Ensure full-width table without collapsing
        },
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

    $.fn.dataTable.ext.errMode = 'none';

    var table = $('.myTable').DataTable();

    // Redraw the table when a filter changes
    $('.date-range-filter').change(function () {
        table.draw();
    });
}
