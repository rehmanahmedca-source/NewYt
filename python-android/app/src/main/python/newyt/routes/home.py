"""Page routes -- render the Jinja2 templates. All data is loaded by JS via
the /api endpoints (routes/api.py), so these views are mostly just shells."""
from flask import Blueprint, render_template

home_bp = Blueprint("home", __name__)


@home_bp.route("/")
def dashboard():
    return render_template("dashboard.html", active_page="dashboard")


@home_bp.route("/downloads")
def downloads():
    return render_template("downloads.html", active_page="downloads")


@home_bp.route("/history")
def history():
    return render_template("history.html", active_page="history")


@home_bp.route("/settings")
def settings():
    return render_template("settings.html", active_page="settings")


@home_bp.route("/about")
def about():
    return render_template("about.html", active_page="about")
