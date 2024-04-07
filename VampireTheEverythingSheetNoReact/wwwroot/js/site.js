// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function dotClicked(element) {
    var traitID = parseInt(element.id.replace("trait-", ""), 10);

    var value = parseInt(element.attributes["data-index"], 10);
    if (element.attributes["data-last-filled"] === "true") {
        value--;
    }

    updateTrait(traitID, value);
}

function updateTrait(traitID, value) {

}