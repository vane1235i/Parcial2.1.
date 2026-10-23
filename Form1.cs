using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace parcial2._1
{
    public partial class Form1 : Form
    {
        // En la captura de SSMS, el servidor aparece como localhost.
        private readonly string cadenaConexion =
            @"Server=localhost;Database=TiendaDB;Integrated Security=True;TrustServerCertificate=True;";

        private int productoSeleccionado = 0;

        public Form1()
        {
            InitializeComponent();

            // Conecta los eventos de los controles.
            // Así no tienes que agregarlos manualmente desde el diseñador.
            Load += Form1_Load;
            dgvProductos.CellClick += dgvProductos_CellClick;
            btnGuardar.Click += btnGuardar_Click;
            btnEditar.Click += btnEditar_Click;
            btnEliminar.Click += btnEliminar_Click;
            btnLimpiar.Click += btnLimpiar_Click;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CargarProductos();
        }

        private void CargarProductos()
        {
            const string consulta = @"
                SELECT
                    p.ID_Producto,
                    p.Nombre,
                    p.precio AS Precio,
                    c.Nombre AS Categoria,
                    p.ID_Categoria
                FROM Producto AS p
                INNER JOIN Categoria AS c
                    ON p.ID_Categoria = c.ID_Categoria
                ORDER BY p.ID_Producto;";

            try
            {
                using (SqlConnection conexion = new SqlConnection(cadenaConexion))
                using (SqlDataAdapter adaptador = new SqlDataAdapter(consulta, conexion))
                {
                    DataTable tabla = new DataTable();
                    adaptador.Fill(tabla);
                    dgvProductos.DataSource = tabla;

                    // Oculta los identificadores, pero quedan disponibles para el código.
                    if (dgvProductos.Columns.Contains("ID_Producto"))
                        dgvProductos.Columns["ID_Producto"].Visible = false;

                    if (dgvProductos.Columns.Contains("ID_Categoria"))
                        dgvProductos.Columns["ID_Categoria"].Visible = false;

                    if (dgvProductos.Columns.Contains("Nombre"))
                        dgvProductos.Columns["Nombre"].HeaderText = "Nombre";

                    if (dgvProductos.Columns.Contains("Precio"))
                        dgvProductos.Columns["Precio"].HeaderText = "Precio";

                    if (dgvProductos.Columns.Contains("Categoria"))
                        dgvProductos.Columns["Categoria"].HeaderText = "Categoría";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudieron cargar los productos.\n\n" + ex.Message,
                    "Error de conexión",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Busca la categoría escrita. Si no existe, la crea.
        private int ObtenerOCrearCategoria(SqlConnection conexion, SqlTransaction transaccion)
        {
            string nombreCategoria = txtCategoria.Text.Trim();

            using (SqlCommand buscar = new SqlCommand(
                "SELECT ID_Categoria FROM Categoria WHERE Nombre = @Nombre;",
                conexion,
                transaccion))
            {
                buscar.Parameters.Add("@Nombre", SqlDbType.VarChar, 50).Value = nombreCategoria;

                object resultado = buscar.ExecuteScalar();

                if (resultado != null)
                    return Convert.ToInt32(resultado);
            }

            using (SqlCommand insertar = new SqlCommand(
                @"INSERT INTO Categoria (Nombre)
                  VALUES (@Nombre);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                conexion,
                transaccion))
            {
                insertar.Parameters.Add("@Nombre", SqlDbType.VarChar, 50).Value = nombreCategoria;
                return Convert.ToInt32(insertar.ExecuteScalar());
            }
        }

        private bool ValidarCampos(out decimal precio)
        {
            precio = 0;

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Escribe el nombre del producto.");
                txtNombre.Focus();
                return false;
            }

            if (!decimal.TryParse(txtPrecio.Text.Trim(), out precio) || precio < 0)
            {
                MessageBox.Show("Escribe un precio válido, por ejemplo: 12.50");
                txtPrecio.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtCategoria.Text))
            {
                MessageBox.Show("Escribe la categoría del producto.");
                txtCategoria.Focus();
                return false;
            }

            return true;
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            decimal precio;

            if (!ValidarCampos(out precio))
                return;

            using (SqlConnection conexion = new SqlConnection(cadenaConexion))
            {
                conexion.Open();
                SqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    int idCategoria = ObtenerOCrearCategoria(conexion, transaccion);

                    using (SqlCommand comando = new SqlCommand(
                        @"INSERT INTO Producto (Nombre, precio, ID_Categoria)
                          VALUES (@Nombre, @Precio, @ID_Categoria);",
                        conexion,
                        transaccion))
                    {
                        comando.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value =
                            txtNombre.Text.Trim();
                        comando.Parameters.Add("@Precio", SqlDbType.Decimal).Value = precio;
                        comando.Parameters["@Precio"].Precision = 18;
                        comando.Parameters["@Precio"].Scale = 2;
                        comando.Parameters.Add("@ID_Categoria", SqlDbType.Int).Value = idCategoria;

                        comando.ExecuteNonQuery();
                    }

                    transaccion.Commit();
                    CargarProductos();
                    LimpiarFormulario();

                    MessageBox.Show("Producto guardado correctamente.");
                }
                catch (Exception ex)
                {
                    transaccion.Rollback();
                    MessageBox.Show("No se pudo guardar el producto.\n\n" + ex.Message);
                }
            }
        }

        private void btnEditar_Click(object sender, EventArgs e)
        {
            if (productoSeleccionado == 0)
            {
                MessageBox.Show("Selecciona un producto de la tabla para editarlo.");
                return;
            }

            decimal precio;

            if (!ValidarCampos(out precio))
                return;

            using (SqlConnection conexion = new SqlConnection(cadenaConexion))
            {
                conexion.Open();
                SqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    int idCategoria = ObtenerOCrearCategoria(conexion, transaccion);

                    using (SqlCommand comando = new SqlCommand(
                        @"UPDATE Producto
                          SET Nombre = @Nombre,
                              precio = @Precio,
                              ID_Categoria = @ID_Categoria
                          WHERE ID_Producto = @ID_Producto;",
                        conexion,
                        transaccion))
                    {
                        comando.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value =
                            txtNombre.Text.Trim();
                        comando.Parameters.Add("@Precio", SqlDbType.Decimal).Value = precio;
                        comando.Parameters["@Precio"].Precision = 18;
                        comando.Parameters["@Precio"].Scale = 2;
                        comando.Parameters.Add("@ID_Categoria", SqlDbType.Int).Value = idCategoria;
                        comando.Parameters.Add("@ID_Producto", SqlDbType.Int).Value =
                            productoSeleccionado;

                        comando.ExecuteNonQuery();
                    }

                    transaccion.Commit();
                    CargarProductos();
                    LimpiarFormulario();

                    MessageBox.Show("Producto actualizado correctamente.");
                }
                catch (Exception ex)
                {
                    transaccion.Rollback();
                    MessageBox.Show("No se pudo actualizar el producto.\n\n" + ex.Message);
                }
            }
        }

        private void dgvProductos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow fila = dgvProductos.Rows[e.RowIndex];

            productoSeleccionado = Convert.ToInt32(fila.Cells["ID_Producto"].Value);
            txtNombre.Text = Convert.ToString(fila.Cells["Nombre"].Value);
            txtPrecio.Text = Convert.ToString(fila.Cells["Precio"].Value);
            txtCategoria.Text = Convert.ToString(fila.Cells["Categoria"].Value);
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (productoSeleccionado == 0)
            {
                MessageBox.Show("Selecciona un producto de la tabla para eliminarlo.");
                return;
            }

            DialogResult confirmar = MessageBox.Show(
                "¿Seguro que deseas eliminar este producto?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmar != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection conexion = new SqlConnection(cadenaConexion))
                using (SqlCommand comando = new SqlCommand(
                    "DELETE FROM Producto WHERE ID_Producto = @ID_Producto;",
                    conexion))
                {
                    comando.Parameters.Add("@ID_Producto", SqlDbType.Int).Value =
                        productoSeleccionado;

                    conexion.Open();
                    comando.ExecuteNonQuery();
                }

                CargarProductos();
                LimpiarFormulario();
                MessageBox.Show("Producto eliminado correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo eliminar el producto.\n\n" + ex.Message);
            }
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            txtNombre.Clear();
            txtPrecio.Clear();
            txtCategoria.Clear();
            productoSeleccionado = 0;
            dgvProductos.ClearSelection();
            txtNombre.Focus();
        }
    }
}