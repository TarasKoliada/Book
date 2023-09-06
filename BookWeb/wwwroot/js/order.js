var dataTable;

$(document).ready(function () {
    loadDataTable();
})

function loadDataTable() {
    dataTable = $('#tableData').DataTable({
        "ajax": { url: '/admin/order/getall' },
        "columns": [
            { data: 'id', "width": "25%" },
            { data: 'name', "width": "15%" },
            { data: 'phoneNumber', "width": "10%" },
            { data: 'applicationUser.email', "width": "15%" },
            { data: 'orderStatus', "width": "10%" },
            { data: 'orderTotal', "width": "10%" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group">
                            <a href="/admin/order/details?orderId=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                            </div>`
                },
                "width": "25%"
            }
        ]
    });
}