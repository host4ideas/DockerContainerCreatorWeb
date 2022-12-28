// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    let inputCount = 1;

    $("#submitContainerCreateForm").click(function (ev) {
        ev.preventDefault();

        var stringArray = new Array();

        for (var i = 1; i <= inputCount; i++) {
            const containerPort = $("#containerPort" + i).val();
            const hostPort = $("#hostPort" + i).val();

            stringArray.push({
                ContainerPort: containerPort,
                HostPort: hostPort
            });
        }

        // collect the form data
        const formData = new FormData(document.getElementById('containerCreateForm'));

        formData.append("mappingPorts", JSON.stringify(stringArray));

        // send the form data to the server using AJAX
        const xhr = new XMLHttpRequest();
        xhr.open('post', '/home/createformcontainer');
        xhr.send(formData);
    });

    $("#rowAdder").click(function () {
        inputCount++;

        newRowAdd =
            '<div id="row"> <div class="input-group my-2">' +
            '<div class="input-group-prepend">' +
            '<button class="btn btn-danger" id="DeleteRow" type="button">' +
            '<i class="bi bi-trash"></i> Delete</button> </div>' +
            '<input type="text" class="form-control m-input" placeholder="eg.: 80/tcp" required' +
            ' id="containerPort' + inputCount + '"' +
            ' name="containerPort' + inputCount + '">' +
            '<input type="text" class="form-control m-input" placeholder="eg.: 8080" required' +
            ' id="hostPort' + inputCount + '"' +
            ' name="hostPort' + inputCount + '"> </div> </div>';

        $('#newinput').append(newRowAdd);
    });

    $("body").on("click", "#DeleteRow", function () {
        inputCount--;
        $(this).parents("#row").remove();
    })
})
